using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UI
{
    public static class CollectionInstantiator
    {
        public static T[] Generate<T>(Transform container, T prefab, int count) where T : Component
        {
            var result = new T[count];

            for (var i = 0; i < count; ++i)
            {
                var view = (T)Object.Instantiate(prefab, container, false);
                view.name += i;

                if (!view.gameObject.activeInHierarchy)
                    view.gameObject.SetActive(true);

                result[i] = view;
            }

            return result;
        }

        public static TV[] Append<TV, TM>(Transform container, TV prefab, IEnumerable<TM> models, Action<TV, TM> connector) where TV : Component
        {
            var modelsArray = models.ToArray();
            var views = Generate(container, prefab, modelsArray.Length);

            for (var i = 0; i < modelsArray.Length; ++i)
                connector(views[i], modelsArray[i]);

            return views;
        }

        public static TV[] Update<TV, TM>(Transform container, IEnumerable<TM> models, Action<TV, TM> connector = null) where TV : Component
        {
            var containerChildCount = container.childCount;

            if (containerChildCount == 0)
                throw new ArgumentException(string.Format("Container {0} is empty. Need at least one child element.", container.name));

            var modelsArray = models.ToArray();
            var result = new TV[modelsArray.Length];

            if (containerChildCount < modelsArray.Length)
                Generate(container, container.GetChild(0).gameObject.GetComponent<TV>(), modelsArray.Length - containerChildCount);
            else
            {
                for (var i = modelsArray.Length; i < containerChildCount; ++i)
                    container.GetChild(i).gameObject.SetActive(false);
            }

            for (var i = 0; i < modelsArray.Length; ++i)
            {
                result[i] = container.GetChild(i).GetComponent<TV>();

                if (!result[i].gameObject.activeInHierarchy)
                    result[i].gameObject.SetActive(true);

                if (connector != null)
                    connector(result[i], modelsArray[i]);
            }

            return result;
        }

        public static TV[] Update<TV, TM>(IList<TV> fixViews, IEnumerable<TM> models, Action<TV, TM> connector = null) where TV : Component
        {
            var containerChildCount = fixViews.Count;
            if (containerChildCount == 0)
                throw new ArgumentException("Container is empty. Need at least one child element.");

            var modelsArray = models.ToArray();
            var result = new TV[modelsArray.Length];

            if (containerChildCount < modelsArray.Length)
            {
                throw new ArgumentException("Container have small elements.", "fixViews");
            }
            else
            {
                for (var i = modelsArray.Length; i < containerChildCount; ++i)
                    fixViews[i].gameObject.SetActive(false);
            }

            for (var i = 0; i < modelsArray.Length; ++i)
            {
                result[i] = fixViews[i].GetComponent<TV>();

                if (!result[i].gameObject.activeInHierarchy)
                    result[i].gameObject.SetActive(true);

                if (connector != null)
                    connector(result[i], modelsArray[i]);
            }

            return result;
        }
    }
}