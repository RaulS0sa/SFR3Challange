using System;
using System.Collections.Concurrent;
using SFR3JobScheduler.JssJobHandler.Types;

namespace SFR3JobScheduler.JssJobHandler
{
	public class JssSessionService
	{
        private readonly ConcurrentDictionary<string, Solution> _sessions = new ConcurrentDictionary<string, Solution>();
        //      public JssSessionService()
        //{
        //}
        public async Task<ConcurrentDictionary<string, Solution>> GetSessions()
        {
            return _sessions;
        }
        public async Task<Solution> GetUnnocupiedRoom(string _game)
        {
            return null;
            //return _sessions.Where(x => x.Value.GameName == _game &&
            //                        x.Value.Players.Count < 8 &&
            //                        x.Value.roomType == RoomType.Competitive &&
            //                        x.Value.Status == GameState.WaitingForPlayers).Select(x => x.Value).FirstOrDefault();
        }
        public async Task<Solution> GetSingleSessions(string key)
        {
            if (key != null)
            {
                if (_sessions.ContainsKey(key))
                {
                    return _sessions[key];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public async Task StoreRoom(string sessionId, Solution connectionId)
        {
            // Store the mapping of session ID to connection ID
            _sessions[sessionId] = connectionId;
        }

        public async Task RemoveRoom(string room_id)
        {
            // Remove the session associated with the connection ID
            var sessionId = _sessions[room_id];
            if (sessionId != null)
            {
                _sessions.TryRemove(room_id, out _);
            }
        }
    }
}

