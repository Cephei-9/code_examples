using System;
using System.Collections.Generic;
using DoodleObby._Gameplay.Drawing.Network;
using Mirror;
using UnityEngine;

namespace DoodleObby._Gameplay.Multiplayer
{
    public class InterestZones : InterestManagement
    {
        [SerializeField] private float _rebuildInterval = 1;
        [SerializeField] private Transform[] _zonePoints;
        
        private Dictionary<NetworkConnectionToClient, int> _connectionZoneMap = new();
        private double _lastRebuildTime;
        
        public int ZoneCount => _zonePoints.Length;

        // private void Awake()
        // {
        //     float minX = float.NegativeInfinity;
        //     
        //     foreach (Transform zonePoint in _zonePoints)
        //     {
        //         if(zonePoint.position.x < minX)
        //             Debug.LogError($"Zone less than should be: {zonePoint.name}");
        //
        //         minX = zonePoint.position.x;
        //     }
        // }

        [ServerCallback]
        private void Update()
        {
            if (NetworkTime.time >= _lastRebuildTime + _rebuildInterval) 
                Rebuild();
        }

        public void Rebuild()
        {
            _lastRebuildTime = NetworkTime.time;
        
            ActualizeConnections();
            RebuildAll();
        }

        public int GetZoneByPosition(Vector3 position)
        {
            int zoneResult = 0;

            for (var i = 0; i < _zonePoints.Length; i++, zoneResult++)
            {
                Vector3 zonePosition = _zonePoints[i].position;

                if (zonePosition.x > position.x)
                    return zoneResult;
            }

            return zoneResult;
        }

        public int GetZoneByPosition(float xPosition)
        {
            return GetZoneByPosition(new Vector3(xPosition, 0, 0));
        }

        public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient newObserver)
        {
            if (newObserver.identity == null)
                return false;
        
            if (!_connectionZoneMap.ContainsKey(newObserver)) 
                AddZoneForConnection(newObserver);
        
            if (identity && identity.isLine)
            {
                //HandlePlayerDraw(identity, new HashSet<NetworkConnectionToClient>(), new [] {newObserver});
                return true;
            }

            return IdentityIsVisible(identity, newObserver);
        }

        public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers)
        {
            IEnumerable<NetworkConnectionToClient> connections = NetworkServer.connections.Values;

            if (identity && identity.ReplicateAll)
            {
                AddAllConnectionToObservers(newObservers, connections);
            }

            if (identity && identity.isLine)
            {
                HandlePlayerDraw(identity, newObservers, connections);
                return;
            }
        
            foreach (NetworkConnectionToClient connection in connections)
            {
                if (IdentityIsVisible(identity, connection))
                    newObservers.Add(connection);
            }
        }

        public override void SetHostVisibility(NetworkIdentity identity, bool visible)
        {
            // Empty realization
        }

        private void HandlePlayerDraw(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers, IEnumerable<NetworkConnectionToClient> connections)
        {
            bool hasPlayerDraw = identity.mainComponent.TryGetComponent(out PlayerDrawFacade playerDraw);

            if (!hasPlayerDraw)
            {
                Debug.LogError("Has not player draw component");
                return;
            }

            int leftZone = GetZoneByPosition(playerDraw.LeftPositionX);
            int rightZone = GetZoneByPosition(playerDraw.RightPositionX);
            HashSet<NetworkConnectionToClient> connectionForDraw = new();

            for (int i = leftZone; i <= rightZone; i++)
            {
                int zoneOfDraw = i;
            
                foreach (NetworkConnectionToClient connection in connections)
                {
                    if (connection == null || connection.identity == null || !_connectionZoneMap.ContainsKey(connection))
                        continue;

                    if (connection.identity == playerDraw.PlayerIdentity)
                    {
                        connectionForDraw.Add(connection);
                        continue;
                    }
                
                    int zoneDelta = Mathf.Abs(zoneOfDraw - _connectionZoneMap[connection]);
                
                    if(zoneDelta <= 1)
                        connectionForDraw.Add(connection);
                }
            }

            playerDraw.UpdateConnections(connectionForDraw);
            AddAllConnectionToObservers(newObservers, connections);
        }

        private void AddAllConnectionToObservers(HashSet<NetworkConnectionToClient> newObservers, IEnumerable<NetworkConnectionToClient> connections)
        {
            foreach (NetworkConnectionToClient connection in connections)
            {
                if(connection.identity != null)
                    newObservers.Add(connection);
            
                if(connection.identity != null != connection.isReady)
                    Debug.Log($"Identity: {connection.identity != null} != isReady: {connection.isReady}");
            }
        }

        private void ActualizeConnections()
        {
            _connectionZoneMap.Clear();
        
            foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
            {
                if (connection.identity != null) 
                    AddZoneForConnection(connection);
            }
        }

        private void AddZoneForConnection(NetworkConnectionToClient connection)
        {
            NetworkIdentity identity = connection.identity;
            int zone = GetZoneByPosition(identity.transform.position);
        
            identity.InterestZone = zone;
            _connectionZoneMap.Add(connection, zone);
        }

        private bool IdentityIsVisible(NetworkIdentity identity, NetworkConnectionToClient connection)
        {
            if (identity == null)
                return false;
        
            if (connection == null || connection.identity == null || !_connectionZoneMap.ContainsKey(connection))
                return false;

            int connectionZone = _connectionZoneMap[connection];
            int identityZone = GetZoneByPosition(identity.transform.position);
        
            int zoneDelta = Mathf.Abs(connectionZone - identityZone);
            return zoneDelta <= 1;
        }
    }
}