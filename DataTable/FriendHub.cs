using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace DataTable
{
    public class FriendHub : Hub
    {
        private static DataContext _db = new DataContext();
        private static ConcurrentDictionary<string, int> _locks = new ConcurrentDictionary<string, int>();
        private static object _lock = new object();

        public override async Task OnConnected()
        {
            var query = from f in _db.Friends
                        orderby f.Name
                        select f;

            await Clients.Caller.all(query);
            await Clients.Caller.allLocks(_locks.Values);
        }

        public override async Task OnReconnected()
        {
            // Refresh as other users could update data while we were offline
            await OnConnected();
        }

        public override async Task OnDisconnected()
        {
            int removed;
            if(_locks.TryRemove(Context.ConnectionId, out removed))
            {
                await Clients.All.allLocks(_locks.Values);
            }
        }

        public void TakeLock(Friend value)
        {
            // Race condition: N clients attempting to edit same row
            lock (_lock)
            {
                foreach (int id in _locks.Values)
                {
                    if (value.Id == id)
                    {
                        return;
                    }
                }

                _locks.AddOrUpdate(Context.ConnectionId, value.Id, (key, oldValue) => value.Id);
                Clients.Caller.takeLockSuccess(value);
                Clients.All.allLocks(_locks.Values);
            }                        
        }
        
        public void Add(Friend value)
        {
            var added = _db.Friends.Add(value);
            _db.SaveChanges();

            Clients.All.add(added);
        }

        public void Delete(Friend value)
        {
            var entity = _db.Friends.First<Friend>(f => f.Id == value.Id);
            var removed = _db.Friends.Remove(entity);
            _db.SaveChanges();

            Clients.All.delete(removed);
        }

        public void Update(Friend value)
        {
            var updated = _db.Friends.First<Friend>(f => f.Id == value.Id);
            updated.Name = value.Name;
            _db.SaveChanges();

            Clients.All.update(updated);

            int removed;
            _locks.TryRemove(Context.ConnectionId, out removed);
            Clients.All.allLocks(_locks.Values);
        }
    }
}