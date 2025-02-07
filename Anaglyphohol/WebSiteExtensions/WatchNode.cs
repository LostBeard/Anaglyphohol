using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace Anaglyphohol.WebSiteExtensions
{
    public class WatchNode
    {
        BlazorJSRuntime JS => BlazorJSRuntime.JS;
        public delegate Element? QuerySelectorDelegate(Document document);
        public string Name { get; set; }
        QuerySelectorDelegate? SelectorFn { get; set; }
        public string Selector { get; set; }
        public bool Found { get; set; }
        public bool CheckVisibility { get; set; }
        public CheckVisibilityOptions? CheckVisibilityOptions { get; set; }
        public Action<WatchNode>? OnFound { get; set; }
        public Action<WatchNode>? OnLost { get; set; }
        public Element? Query(Document? document) => Query<Element>(document);
        public ShadowRootQueryMode ShadowRootQueryMode { get; set; } = ShadowRootQueryMode.Strict;
        public TElement? Query<TElement>(Document? document) where TElement : Element
        {
            if (document == null) return null;
            if (SelectorFn != null)
            {
                var el = SelectorFn(document);
                return el?.JSRefMove<TElement>();
            }
            TElement? ret = null;
            var nodeList = document?.DeepQuerySelectorAll<TElement>(Selector, ShadowRootQueryMode);
            if (nodeList != null && nodeList.Count > 0)
            {
                if (CheckVisibility)
                {
                    foreach (TElement element in nodeList)
                    {
                        if (ret == null && element.CheckVisibility(CheckVisibilityOptions))
                        {
                            ret = element;
                        }
                        else
                        {
                            element.Dispose();
                        }
                    }
                }
                else
                {
                    ret = nodeList[0];
                }
            }
            return ret;
        }
        List<TElement> QueryAll<TElement>(Document? document) where TElement : Element
        {
            var ret = new List<TElement>();
            var nodeList = document?.DeepQuerySelectorAll<TElement>(Selector, ShadowRootQueryMode);
            if (nodeList != null && nodeList.Count > 0)
            {
                if (CheckVisibility)
                {
                    foreach (TElement element in nodeList)
                    {
                        if (element.CheckVisibility(CheckVisibilityOptions))
                        {
                            ret.Add(element);
                        }
                        else
                        {
                            element.Dispose();
                        }
                    }
                }
                else
                {
                    ret.AddRange(nodeList);
                }
            }
            return ret;
        }
        public WatchNode(string name, QuerySelectorDelegate selectorFn, bool checkVisibility = false, Action<WatchNode>? onFound = null, Action<WatchNode>? onLost = null)
        {
            Name = name;
            SelectorFn = selectorFn;
            Selector = "";
            OnFound = onFound;
            OnLost = onLost;
            CheckVisibility = checkVisibility;
        }
        public WatchNode(string name, string selector, bool checkVisibility = false, Action<WatchNode>? onFound = null, Action<WatchNode>? onLost = null)
        {
            Name = name;
            Selector = selector;
            OnFound = onFound;
            OnLost = onLost;
            CheckVisibility = checkVisibility;
        }
        public WatchNode(string name, string selector, CheckVisibilityOptions checkVisibilityOptions, Action<WatchNode>? onFound = null, Action<WatchNode>? onLost = null)
        {
            Name = name;
            Selector = selector;
            OnFound = onFound;
            OnLost = onLost;
            CheckVisibility = checkVisibilityOptions != null;
            CheckVisibilityOptions = checkVisibilityOptions;
        }
    }
}
