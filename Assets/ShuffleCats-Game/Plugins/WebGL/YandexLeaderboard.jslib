mergeInto(LibraryManager.library, {
  YG_LeaderboardRequest: function (jsonPtr) {
    var request = JSON.parse(UTF8ToString(jsonPtr));
    var sdk = window.ysdk;
    var send = window.ygSend;
    if (!window.ygLeaderboardState) {
      window.ygLeaderboardState = { queue: Promise.resolve(), boards: {} };
    }
    var state = window.ygLeaderboardState;
    var result = { id: request.id, status: "ok", authorized: false, authorizationKnown: false, userRank: 0,
      bestScore: 0, retryAfter: 0, scoreStatus: "", entries: [] };

    function errorCode(error) {
      var code = String(error && (error.code || error.message) || error);
      if (/LEADERBOARD_PLAYER_NOT_PRESENT/.test(code)) return "noEntry";
      if (/404|LEADERBOARD_NOT_FOUND|not found/i.test(code)) return "notConfigured";
      if (/429|limit|too many/i.test(code)) return "rateLimited";
      if (/timeout/i.test(code)) return "timeout";
      return "error";
    }

    // SDK network failures must not leave the modal loading forever. Dialogs
    // are deliberately excluded: their lifetime belongs to Yandex and the user.
    function bounded(promise) {
      return new Promise(function (resolve, reject) {
        var timer = setTimeout(function () { reject(new Error("timeout")); }, 8000);
        Promise.resolve(promise).then(function (value) {
          clearTimeout(timer); resolve(value);
        }, function (error) { clearTimeout(timer); reject(error); });
      });
    }

    function available(method) {
      return bounded(sdk.isAvailableMethod("leaderboards." + method));
    }

    function getPlayer() {
      return bounded(sdk.getPlayer({ scopes: false })).catch(function () { return null; });
    }

    function isCurrent() { return window.ysdk === sdk && window.ygLeaderboardState === state; }

    function run() {
      if (!isCurrent()) return;
      if (!sdk || !sdk.leaderboards) { result.status = "unavailable"; return; }
      return getPlayer().then(function (player) {
        if (!isCurrent()) return null;
        result.authorizationKnown = !!player;
        result.authorized = !!(player && player.isAuthorized());
        if (request.action === "authorize" && !player) {
          result.status = "error";
          return null;
        }
        if (request.action !== "authorize" || (player && player.isAuthorized())) return player;
        send("OnAuthDialogState", "1");
        return Promise.resolve().then(function () { return sdk.auth.openAuthDialog(); }).then(function () {
          return getPlayer();
        }).catch(function () {
          result.status = "cancelled";
          return null;
        }).then(function (authorizedPlayer) {
          send("OnAuthDialogState", "0");
          return authorizedPlayer;
        });
      }).then(function (player) {
        if (!isCurrent() || result.status === "cancelled") return;
        result.authorizationKnown = !!player;
        result.authorized = !!(player && player.isAuthorized());
        // Use the freshly acquired Player for rankings. Do not replace the
        // cloud-save Player here: its snapshot was loaded at boot, and rebinding
        // it without loading the account save could overwrite existing progress.
        // Bootstrap reinitializes and reconciles cloud progress on the next launch.
        var userId = result.authorized ? player.getUniqueID() : "guest";
        var key = request.name + ":" + userId;
        var board = state.boards[key];
        if (!board) board = state.boards[key] = { readAt: -Infinity, entryAt: -Infinity,
          writeAt: -Infinity, best: 0, cache: null };

        function submit() {
          if (!result.authorized) {
            result.scoreStatus = result.authorizationKnown ? "unauthorized" : "error";
            return Promise.resolve();
          }
          if (!Number.isSafeInteger(request.score) || request.score < 0) throw new Error("invalid score");
          if (request.score === 0) return Promise.resolve();
          // At most 60 getPlayerEntry calls / 5 min, and one setScore / second.
          if (Date.now() - board.entryAt < 5100) {
            result.bestScore = board.best;
            if (request.score > board.best) result.scoreStatus = "rateLimited";
            return Promise.resolve();
          }
          return available("getPlayerEntry").then(function (allowed) {
            if (!isCurrent()) return null;
            if (!allowed) { result.scoreStatus = "unsupported"; return null; }
            board.entryAt = Date.now();
            return bounded(sdk.leaderboards.getPlayerEntry(request.name)).catch(function (error) {
              // Only this documented error means it is safe to create a first
              // score. A network error must never overwrite a higher record.
              if (errorCode(error) === "noEntry") return { score: 0 };
              throw error;
            });
          }).then(function (entry) {
            if (!isCurrent() || !entry) return;
            board.best = Math.max(board.best, entry.score);
            result.bestScore = board.best;
            if (request.score <= board.best) return;
            return available("setScore").then(function (allowed) {
              if (!isCurrent()) return;
              if (!allowed) { result.scoreStatus = "unsupported"; return; }
              if (Date.now() - board.writeAt < 1100) { result.scoreStatus = "rateLimited"; return; }
              board.writeAt = Date.now();
              return bounded(sdk.leaderboards.setScore(request.name, request.score)).then(function () {
                board.best = request.score;
                result.bestScore = request.score;
              });
            });
          }).catch(function (error) { result.scoreStatus = errorCode(error); });
        }

        function read() {
          if (!isCurrent()) return;
          // 20 getEntries requests / 5 minutes. A single global read clock also
          // covers guest -> authenticated transitions and rapid panel toggles.
          var remaining = 16000 - (Date.now() - (state.readAt === undefined ? -Infinity : state.readAt));
          if (remaining > 0) {
            result.retryAfter = Math.ceil(remaining / 1000);
            if (board.cache) {
              result.entries = board.cache.entries;
              result.userRank = board.cache.userRank;
              result.bestScore = Math.max(result.bestScore, board.best);
              return;
            }
            result.status = "rateLimited";
            return;
          }
          state.readAt = Date.now();
          return bounded(sdk.leaderboards.getEntries(request.name, {
            quantityTop: 10, includeUser: result.authorized, quantityAround: 2
          })).then(function (data) {
            result.userRank = Number(data.userRank) || 0;
            result.entries = (data.entries || []).map(function (entry) {
              return { rank: entry.rank, score: entry.score,
                name: entry.player.publicName || "", isUser: result.authorized && entry.player.uniqueID === userId };
            });
            result.entries.sort(function (a, b) { return a.rank - b.rank; });
            result.entries.forEach(function (entry) {
              if (entry.isUser) board.best = Math.max(board.best, entry.score);
            });
            result.bestScore = Math.max(result.bestScore, board.best);
            board.cache = { entries: result.entries, userRank: result.userRank };
            result.retryAfter = 16;
          });
        }

        return submit().then(function () {
          if (request.action === "submit") {
            result.status = result.scoreStatus || "ok";
            return;
          }
          return read();
        });
      });
    }

    state.queue = state.queue.then(run).catch(function (error) {
      result.status = errorCode(error);
      result.retryAfter = 16;
    }).then(function () {
      send("OnLeaderboardResult", JSON.stringify(result));
    });
  }
});
