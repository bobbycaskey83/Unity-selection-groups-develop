using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System;

namespace Unity.SelectionGroups
{
    [System.Serializable]
    public partial class SelectionGroup : IEnumerable<UnityEngine.Object>
    {
        public string name;
        public Color color;
        public bool showMembers;
        public string query = string.Empty;
        public bool sort = false;

        public int groupId;

        [System.NonSerialized] List<UnityEngine.Object> members = new List<UnityEngine.Object>();

        HashSet<GlobalObjectId> globalObjectIdSet = new HashSet<GlobalObjectId>();

        GoQL.GoQLExecutor executor = new GoQL.GoQLExecutor();

        public int Count => members.Count;

        public void RefreshQueryResults()
        {
            if (query != string.Empty)
            {
                executor.Code = query;
                var objects = executor.Execute();
                members.Clear();
                if (sort)
                    System.Array.Sort(objects, (a, b) => a.name.CompareTo(b.name));
                members.AddRange(objects);
                SortMembers();
            }
        }

        internal void Add(IEnumerable<UnityEngine.Object> objectReferences)
        {
            members.AddRange(objectReferences);
            SortMembers();
        }

        void SortMembers()
        {
            members.Sort((A, B) => A.name.CompareTo(B.name));
        }

        internal void ConvertSceneObjectsToGlobalObjectIds()
        {
            globalObjectIdSet.UnionWith(GetGlobalObjectIds(members.ToArray()));
        }

        internal void Clear()
        {
            globalObjectIdSet.ExceptWith(GetGlobalObjectIds(members.ToArray()));
            members.Clear();
        }

        internal void ConvertGlobalObjectIdsToSceneObjects()
        {
            var outputObjects = new UnityEngine.Object[globalObjectIdSet.Count];
            GlobalObjectId.GlobalObjectIdentifiersToObjectsSlow(globalObjectIdSet.ToArray(), outputObjects);
            var objectSet = new HashSet<UnityEngine.Object>(members);
            foreach (var i in outputObjects)
            {
                if (i != null) objectSet.Add(i);
            }
            members.Clear();
            members.AddRange(objectSet);
            SortMembers();
        }

        internal void Remove(UnityEngine.Object[] objects)
        {
            globalObjectIdSet.ExceptWith(GetGlobalObjectIds(objects));
            foreach (var o in objects) members.Remove(o);
        }

        internal GlobalObjectId[] GetGlobalObjectIds(params UnityEngine.Object[] gameObjects)
        {
            var gids = new GlobalObjectId[gameObjects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(gameObjects, gids);
            return gids;
        }

        public IEnumerator<UnityEngine.Object> GetEnumerator()
        {
            return members.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return members.GetEnumerator();
        }
    }
}