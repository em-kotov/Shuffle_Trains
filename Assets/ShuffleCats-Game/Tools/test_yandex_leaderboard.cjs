// node --test Tools/test_yandex_leaderboard.cjs
// Tests the production WebGL bridge with SDK doubles, not a real Yandex account.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');
const source = fs.readFileSync(path.join(__dirname, '../Plugins/WebGL/YandexLeaderboard.jslib'), 'utf8');
const flush = () => new Promise(resolve => setImmediate(resolve));

function setup(options = {}) {
  const h = { authorized: options.authorized ?? false, calls: [], messages: [], score: options.score ?? 0, now: 0 };
  const timers = new Map();
  let timerId = 0;
  const player = { isAuthorized: () => h.authorized, getUniqueID: () => 'me' };
  const sdk = {
    getPlayer: async () => {
      h.calls.push('player');
      if (options.playerError) throw new Error('network error');
      return player;
    },
    isAvailableMethod: async method => { h.calls.push(method); return options.available !== false; },
    auth: { openAuthDialog: async () => {
      h.calls.push('auth');
      if (options.cancel) throw new Error('cancelled');
      h.authorized = true;
    } },
    leaderboards: {
      getPlayerEntry: async name => {
        assert.equal(name, 'levelsCompleted');
        h.calls.push('entry');
        if (options.entryError) throw { code: options.entryError };
        return { score: h.score };
      },
      setScore: async (name, score) => { h.calls.push(['set', name, score]); h.score = score; },
      getEntries: async (name, query) => {
        assert.equal(name, 'levelsCompleted');
        h.calls.push(['read', query]);
        if (options.readError) throw new Error(options.readError);
        if (options.hang) return new Promise(() => {});
        return { userRank: h.authorized ? 12 : 0, entries: [
          { rank: 1, score: 30, player: { publicName: 'Winner', uniqueID: 'top' } },
          { rank: 12, score: h.score, player: { publicName: '<b>me</b>', uniqueID: 'me' } }
        ] };
      }
    }
  };
  const window = { ysdk: options.missing ? null : sdk, ygSend: (name, payload) => h.messages.push([name, payload]) };
  const context = { window, LibraryManager: { library: {} }, mergeInto: Object.assign,
    UTF8ToString: value => value, Date: { now: () => h.now },
    setTimeout: (fn, delay) => { timers.set(++timerId, { fn, at: h.now + delay }); return timerId; },
    clearTimeout: id => timers.delete(id) };
  vm.runInNewContext(source, context);
  h.request = async (action = 'read', score = 5) => {
    const id = h.messages.length + 100;
    context.LibraryManager.library.YG_LeaderboardRequest(JSON.stringify({ id, name: 'levelsCompleted', action, score }));
    await flush();
    const message = h.messages.find(m => m[0] === 'OnLeaderboardResult' && JSON.parse(m[1]).id === id);
    return message ? JSON.parse(message[1]) : null;
  };
  h.advance = async milliseconds => {
    h.now += milliseconds;
    for (const [id, timer] of timers) if (timer.at <= h.now) { timers.delete(id); timer.fn(); }
    await flush();
  };
  h.window = window;
  return h;
}

test('guests can read rankings without a login dialog or score submission', async () => {
  const h = setup();
  const result = await h.request();
  assert.equal(result.status, 'ok');
  assert.equal(result.authorized, false);
  assert.equal(h.calls.includes('auth'), false);
  assert.equal(h.calls.includes('entry'), false);
  assert.equal(h.calls.find(c => c[0] === 'read')[1].includeUser, false);
  assert.equal(result.entries.some(e => e.isUser), false);
});

test('submission is restricted to authorized players', async () => {
  const h = setup();
  assert.equal((await h.request('submit')).status, 'unauthorized');
  assert.equal(h.calls.some(c => Array.isArray(c)), false);
});

test('only the explicit authorize action opens login, brackets pause, and reinitializes Player', async () => {
  const h = setup();
  const result = await h.request('authorize');
  assert.equal(result.authorized, true);
  assert.deepEqual(h.calls.slice(0, 3), ['player', 'auth', 'player']);
  assert.deepEqual(h.messages.slice(0, 2), [['OnAuthDialogState', '1'], ['OnAuthDialogState', '0']]);
  assert.equal(result.entries[1].isUser, true);
  assert.equal(result.userRank, 12);
  assert.equal(h.score, 5);
});

test('login cancellation releases its pause and never writes a score', async () => {
  const h = setup({ cancel: true });
  assert.equal((await h.request('authorize')).status, 'cancelled');
  assert.deepEqual(h.messages[1], ['OnAuthDialogState', '0']);
  assert.equal(h.calls.includes('entry'), false);
});

test('already-authorized players never get a second auth prompt', async () => {
  const h = setup({ authorized: true });
  await h.request('authorize');
  assert.equal(h.calls.includes('auth'), false);
  await h.request('authorize');
  assert.equal(h.calls.includes('auth'), false);
});

test('a higher remote score is never lowered by replayed levels or another device', async () => {
  const h = setup({ authorized: true, score: 25 });
  const result = await h.request('submit', 7);
  assert.equal(result.status, 'ok');
  assert.equal(result.bestScore, 25);
  assert.equal(h.calls.some(c => c[0] === 'set'), false);
});

test('a new record checks availability before getPlayerEntry and setScore', async () => {
  const h = setup({ authorized: true });
  assert.equal((await h.request('submit', 30)).status, 'ok');
  assert.deepEqual(h.calls.slice(1), ['leaderboards.getPlayerEntry', 'entry', 'leaderboards.setScore', ['set', 'levelsCompleted', 30]]);
});

test('only LEADERBOARD_PLAYER_NOT_PRESENT allows first-score creation', async () => {
  const first = setup({ authorized: true, entryError: 'LEADERBOARD_PLAYER_NOT_PRESENT' });
  assert.equal((await first.request('submit')).status, 'ok');
  assert.equal(first.score, 5);
  const failed = setup({ authorized: true, entryError: 'NETWORK_ERROR' });
  const result = await failed.request();
  assert.equal(result.status, 'ok');
  assert.equal(result.scoreStatus, 'error');
  assert.equal(failed.score, 0);
});

test('unavailable authorized methods do not trigger forbidden SDK calls', async () => {
  const h = setup({ authorized: true, available: false });
  assert.equal((await h.request('submit')).status, 'unsupported');
  assert.equal(h.calls.includes('entry'), false);
});

test('reopening uses cached data and rate limits across guest-to-account changes', async () => {
  const h = setup();
  await h.request();
  await h.request();
  assert.equal(h.calls.filter(c => c[0] === 'read').length, 1);
  const signedIn = await h.request('authorize');
  assert.equal(signedIn.status, 'rateLimited');
  assert.equal(signedIn.authorized, true);
  await h.advance(16001);
  const refreshed = await h.request();
  assert.equal(refreshed.status, 'ok');
  assert.equal(h.calls.filter(c => c[0] === 'read').length, 2);
});

test('rapid new records respect getPlayerEntry and setScore request budgets', async () => {
  const h = setup({ authorized: true });
  await h.request('submit', 3);
  assert.equal((await h.request('submit', 4)).status, 'rateLimited');
  await h.advance(5101);
  assert.equal((await h.request('submit', 4)).status, 'ok');
  assert.equal(h.score, 4);
});

test('missing console board and connection errors are reported as retryable states', async () => {
  assert.equal((await setup({ readError: '404 not found' }).request()).status, 'notConfigured');
  assert.equal((await setup({ readError: 'network error' }).request()).status, 'error');
  assert.equal((await setup({ missing: true }).request()).status, 'unavailable');
});

test('a stalled SDK request times out instead of keeping the panel loading', async () => {
  const h = setup({ hang: true });
  assert.equal(await h.request(), null);
  await h.advance(8001);
  const result = JSON.parse(h.messages.at(-1)[1]);
  assert.equal(result.status, 'timeout');
});

test('failed authorization lookup does not offer login to a possibly signed-in player', async () => {
  const h = setup({ playerError: true });
  const result = await h.request();
  assert.equal(result.status, 'ok');
  assert.equal(result.authorizationKnown, false);
  assert.equal(h.calls.includes('auth'), false);
  await h.request('authorize');
  assert.equal(h.calls.includes('auth'), false);
});

test('leaderboard login does not rebind cloud saves before the account snapshot is loaded', async () => {
  const h = setup();
  const bootPlayer = {};
  h.window.ygPlayer = bootPlayer;
  await h.request('authorize');
  assert.equal(h.window.ygPlayer, bootPlayer);
});

test('leaderboard messages all have matching Unity bridge receivers', () => {
  const bridge = fs.readFileSync(path.join(__dirname, '../Scripts/Yandex/YandexBridge.cs'), 'utf8');
  for (const [, method] of source.matchAll(/send\("([A-Za-z]+)"/g))
    assert.match(bridge, new RegExp('public void ' + method + '\\(string '));
});

test('an account record is displayed even with an empty local save, including cached reads', async () => {
  const h = setup({ authorized: true, score: 25 });
  assert.equal((await h.request('read', 0)).bestScore, 25);
  assert.equal((await h.request('read', 0)).bestScore, 25);
  assert.equal(h.calls.some(c => c[0] === 'set'), false);
});
