// Run with: node Tools/test_yandex_bridge.cjs (no packages required).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const source = fs.readFileSync(path.join(__dirname, '../Plugins/WebGL/YandexGames.jslib'), 'utf8');

function events() {
  const listeners = new Map();
  return {
    addEventListener(name, fn) {
      if (!listeners.has(name)) listeners.set(name, new Set());
      listeners.get(name).add(fn);
    },
    removeEventListener(name, fn) { listeners.get(name)?.delete(fn); },
    emit(name) { for (const fn of listeners.get(name) || []) fn(); },
    count(name) { return listeners.get(name)?.size || 0; }
  };
}

function setup({ hidden = false, focused = true, missing = false, delayedMessages = false } = {}) {
  const window = events();
  const document = Object.assign(events(), { hidden, focused, hasFocus() { return this.focused; } });
  const platform = events();
  const messages = [];
  const timers = [];
  const intervals = new Map();
  let intervalId = 0;
  let resolvePlayer;
  const player = new Promise(resolve => { resolvePlayer = resolve; });
  const ads = {};
  const sdk = {
    on: platform.addEventListener,
    off: platform.removeEventListener,
    getPlayer: () => player,
    adv: {
      showFullscreenAdv: options => { ads.fullscreen = options.callbacks; },
      showRewardedVideo: options => { ads.rewarded = options.callbacks; }
    }
  };
  const context = {
    window, document, LibraryManager: { library: {} },
    mergeInto: Object.assign,
    setTimeout: fn => { timers.push(fn); },
    setInterval: fn => { intervals.set(++intervalId, fn); return intervalId; },
    clearInterval: id => { intervals.delete(id); }
  };
  const receive = (target, method, value) => {
    assert.equal(target, 'YandexGamesBridge');
    messages.push([method, value]);
  };
  if (!delayedMessages) context.SendMessage = receive;
  if (!missing) context.YaGames = { init: () => Promise.resolve(sdk) };
  vm.runInNewContext(source, context);
  const api = context.LibraryManager.library;
  api.YG_Init();
  const pollFocus = () => { for (const fn of intervals.values()) fn(); };
  return { window, document, platform, sdk, messages, timers, intervals, pollFocus, ads, api, resolvePlayer, receive };
}

const flush = () => new Promise(resolve => setImmediate(resolve));

test('subscribes before player data resolves and handles startup ads', async () => {
  const h = setup();
  await flush();
  assert.equal(h.platform.count('game_api_pause'), 1);
  assert.equal(h.platform.count('game_api_resume'), 1);
  assert.equal(h.messages.some(([name]) => name === 'OnSdkInitialized'), false);
  h.platform.emit('game_api_pause');
  h.platform.emit('game_api_resume');
  assert.deepEqual(h.messages.slice(-2), [['OnGameApiPause', ''], ['OnGameApiResume', '']]);
  h.resolvePlayer({});
  await flush();
  assert.deepEqual(h.messages.at(-1), ['OnSdkInitialized', '']);
});

test('initial background state, switching tabs, minimizing, and window focus without SDK', () => {
  const h = setup({ hidden: true, missing: true });
  assert.deepEqual(h.messages[0], ['SetBrowserFocus', '0']);
  h.window.emit('focus');
  assert.deepEqual(h.messages.filter(([name]) => name === 'SetBrowserFocus'), [['SetBrowserFocus', '0']]);
  h.document.hidden = false;
  h.document.emit('visibilitychange');
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '1']);
  h.document.focused = false;
  h.window.emit('blur');
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '0']);
  h.document.focused = true;
  h.window.emit('focus');
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '1']);
  h.document.hidden = true;
  h.document.emit('visibilitychange');
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '0']);
});

test('visible but unfocused startup waits for actual document focus', () => {
  const h = setup({ focused: false });
  assert.deepEqual(h.messages[0], ['SetBrowserFocus', '0']);
  h.document.focused = true;
  h.window.emit('focus');
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '1']);
});

test('Alt-Tab pauses a visible page and visibility cannot clear focus loss', () => {
  const h = setup();
  h.document.focused = false;
  h.window.emit('blur');
  assert.equal(h.document.hidden, false);
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '0']);
  h.document.hidden = true;
  h.document.emit('visibilitychange');
  h.document.hidden = false;
  h.document.emit('visibilitychange');
  h.window.emit('focus'); // A focus event alone is not proof the document has focus.
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '0']);
  h.document.focused = true;
  h.window.emit('focus');
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '1']);
});

test('focus polling recovers missed window events without flooding Unity', () => {
  const h = setup({ missing: true });
  h.document.focused = false; // No blur or visibilitychange reaches the iframe.
  h.pollFocus();
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '0']);
  const count = h.messages.length;
  for (let i = 0; i < 10; i++) h.pollFocus();
  assert.equal(h.messages.length, count);
  h.document.focused = true; // Also recover a missed focus event on return.
  h.pollFocus();
  assert.deepEqual(h.messages.at(-1), ['SetBrowserFocus', '1']);
});

test('SDK resume checks focus before releasing the platform pause', async () => {
  const h = setup();
  await flush();
  h.platform.emit('game_api_pause');
  h.document.focused = false;
  h.platform.emit('game_api_resume');
  assert.deepEqual(h.messages.slice(-2), [['SetBrowserFocus', '0'], ['OnGameApiResume', '']]);
});

test('early events wait for the Unity instance instead of being lost', async () => {
  const h = setup({ hidden: true, delayedMessages: true });
  await flush();
  h.platform.emit('game_api_pause');
  assert.equal(h.messages.length, 0);
  h.window.unityInstance = { SendMessage: h.receive };
  for (const timer of h.timers.splice(0)) timer();
  assert.deepEqual(h.messages, [['SetBrowserFocus', '0'], ['OnGameApiPause', '']]);
});

test('dispose and reinitialization do not duplicate listeners or deliver old events', async () => {
  const h = setup();
  await flush();
  h.api.YG_Init();
  await flush();
  assert.equal(h.platform.count('game_api_pause'), 1);
  assert.equal(h.document.count('visibilitychange'), 1);
  assert.equal(h.intervals.size, 1);
  h.api.YG_Dispose();
  assert.equal(h.intervals.size, 0);
  const count = h.messages.length;
  h.platform.emit('game_api_pause');
  h.document.emit('visibilitychange');
  h.window.emit('blur');
  h.resolvePlayer({});
  await flush();
  assert.equal(h.messages.length, count);
  assert.equal(h.platform.count('game_api_resume'), 0);
});

test('fullscreen ads stay open indefinitely and terminal callbacks complete once', async () => {
  const h = setup();
  await flush();
  h.api.YG_ShowFullscreenAdv();
  h.ads.fullscreen.onOpen();
  // No timer is allowed to infer an ad has closed.
  assert.equal(h.timers.length, 0);
  assert.equal(h.messages.some(([name]) => name === 'OnAdvClosed'), false);
  h.ads.fullscreen.onClose(true);
  h.ads.fullscreen.onError(new Error('late error'));
  h.ads.fullscreen.onClose(false);
  assert.deepEqual(h.messages.filter(([name]) => name === 'OnAdvClosed'), [['OnAdvClosed', '1']]);
});

test('no-fill, offline, thrown SDK errors, and missing SDK all release the ad request', async () => {
  for (const terminal of ['onClose', 'onError', 'onOffline']) {
    const h = setup();
    await flush();
    h.api.YG_ShowFullscreenAdv();
    h.ads.fullscreen[terminal](false);
    assert.deepEqual(h.messages.at(-1), ['OnAdvClosed', '0']);
  }
  const h = setup();
  await flush();
  h.sdk.adv.showFullscreenAdv = () => { throw new Error('offline'); };
  h.api.YG_ShowFullscreenAdv();
  assert.deepEqual(h.messages.at(-1), ['OnAdvClosed', '0']);
  h.sdk.adv.showRewardedVideo = () => { throw new Error('offline'); };
  h.api.YG_ShowRewardedVideo();
  assert.deepEqual(h.messages.at(-1), ['OnRewardedClosed', '0']);
  const missing = setup({ missing: true });
  missing.api.YG_ShowFullscreenAdv();
  assert.deepEqual(missing.messages.at(-1), ['OnAdvClosed', '0']);
});

test('reward callback alone cannot resume the game and rewards complete only once', async () => {
  const h = setup();
  await flush();
  h.api.YG_ShowRewardedVideo();
  h.ads.rewarded.onOpen();
  h.ads.rewarded.onRewarded();
  assert.equal(h.messages.some(([name]) => name === 'OnRewardedClosed'), false);
  h.ads.rewarded.onClose();
  h.ads.rewarded.onClose();
  assert.deepEqual(h.messages.filter(([name]) => name === 'OnRewardedClosed'), [['OnRewardedClosed', '1']]);
  h.api.YG_ShowRewardedVideo();
  h.ads.rewarded.onClose();
  assert.deepEqual(h.messages.at(-1), ['OnRewardedClosed', '0']);
});

test('all JS messages resolve to public Unity bridge receivers', async () => {
  const bridge = fs.readFileSync(path.join(__dirname, '../Scripts/Yandex/YandexBridge.cs'), 'utf8');
  for (const [, method] of source.matchAll(/(?:send|ygSend)\("([A-Za-z]+)"/g)) {
    assert.match(bridge, new RegExp(`public void ${method}\\(string `));
  }
});
