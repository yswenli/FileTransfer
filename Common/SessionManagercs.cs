using FileTransfer.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FileTransfer.Common
{
    public static class SessionManagercs
    {
        static Dictionary<string, UserToken> _session = new Dictionary<string, UserToken>();

        public static object SyncObject = new object();

        private static int _timeOut = 120;

        static SessionManagercs()
        {
            new Thread(new ThreadStart(() =>
            {
                var removeList = new List<string>();

                while (true)
                {
                    lock (SyncObject)
                    {
                        if (_session != null)
                        {
                            if (_session.Values != null && _session.Values.Count > 0)
                            {
                                foreach (var item in _session.Values)
                                {
                                    if (item.Actived.AddSeconds(_timeOut) <= DateTimeHelper.Now)
                                    {
                                        try
                                        {
                                            item.Socket.Close();
                                        }
                                        catch { }
                                        item.Socket = null;
                                        removeList.Add(item.ID);
                                    }
                                }
                            }
                        }

                        if (removeList.Count > 0)
                        {
                            foreach (var ID in removeList)
                            {
                                _session.Remove(ID);
                            }
                        }

                    }
                    Thread.Sleep(30 * 1000);
                }
            }))
            { IsBackground = true }.Start();
        }

        public static void Add(UserToken userToken)
        {
            lock (SyncObject)
            {
                _session.Add(userToken.ID, userToken);
            }
        }

        public static void Active(string ID)
        {
            lock (SyncObject)
            {
                if (_session.ContainsKey(ID))
                {
                    _session[ID].Actived = DateTimeHelper.Now;
                }
            }
        }

        public static UserToken Get(string ID)
        {
            lock (SyncObject)
            {
                if (_session.ContainsKey(ID))
                {
                    return _session[ID];
                }
                else
                {
                    return null;
                }
            }
        }

        public static void Remove(string ID)
        {
            lock (SyncObject)
            {
                if (_session.ContainsKey(ID))
                {
                    _session.Remove(ID);
                }
            }
        }

        public static List<UserToken> ToList()
        {
            lock (SyncObject)
            {
                if (_session != null)
                {
                    if (_session.Values != null && _session.Values.Count > 0)
                    {
                        var list = new List<UserToken>();
                        foreach (var item in _session.Values)
                        {
                            list.Add(item);
                        }
                        return list;
                    }
                }
                return null;
            }
        }
    }
}
