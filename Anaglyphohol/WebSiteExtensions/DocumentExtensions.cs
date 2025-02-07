using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace Anaglyphohol.WebSiteExtensions
{
    public enum ShadowRootQueryMode
    {
        /// <summary>
        /// Query the shadowRoot only on elements matching a selector with "::shadow"
        /// </summary>
        Strict,
        /// <summary>
        /// Query the shadowRoot and the non-shadow dom on elements matching a selector with "::shadow"
        /// </summary>
        Loose,
        /// <summary>
        /// Query the shadowRoot and the non-shadow dom on all elements matched in the selector
        /// </summary>
        Wide,
    }
    public static class DocumentExtensions
    {
        public static List<T> DeepQuerySelectorAll<T>(this Document document, string selector, ShadowRootQueryMode shadowRootMode = ShadowRootQueryMode.Strict) where T : Node
        {
            return DeepQuerySelectorAll(document, selector, shadowRootMode).Select(el => el.JSRefMove<T>()).ToList();
        }
        public static List<T> DeepQuerySelectorAll<T>(this ShadowRoot document, string selector, ShadowRootQueryMode shadowRootMode = ShadowRootQueryMode.Strict) where T : Node
        {
            return DeepQuerySelectorAll(document, selector, shadowRootMode).Select(el => el.JSRefMove<T>()).ToList();
        }
        public static List<T> DeepQuerySelectorAll<T>(this Element document, string selector, ShadowRootQueryMode shadowRootMode = ShadowRootQueryMode.Strict) where T : Node
        {
            return DeepQuerySelectorAll(document, selector, shadowRootMode).Select(el => el.JSRefMove<T>()).ToList();
        }
        public static List<Element> DeepQuerySelectorAll(this Document document, string selector, ShadowRootQueryMode shadowRootMode = ShadowRootQueryMode.Strict)
        {
            var splitOn = shadowRootMode == ShadowRootQueryMode.Wide ? " " : "::shadow";
            if (shadowRootMode == ShadowRootQueryMode.Wide)
            {
                selector = selector.Replace("::shadow", "");
            }
            var partials = selector.Split(splitOn, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var elems = document.QuerySelectorAll<Element>(partials[0]).Using(nodeList => nodeList.ToList());
            for (var i = 1; i < partials.Length; i++)
            {
                var partial = partials[i];
                var elemsInside = new List<Element>();
                for (var j = 0; j < elems.Count; j++)
                {
                    var el = elems[j];
                    using var shadow = el.ShadowRoot;
                    if (shadow != null)
                    {
                        var nodeList = shadow.QuerySelectorAll<Element>(partial).Using(nodeList => nodeList.ToList());
                        elemsInside.AddRange(nodeList);
                    }
                    if (shadowRootMode != ShadowRootQueryMode.Strict)
                    {
                        var nodeList = el.QuerySelectorAll<Element>(partial).Using(nodeList => nodeList.ToList());
                        elemsInside.AddRange(nodeList);
                    }
                }
                elems = elemsInside;
            }
            return elems;
        }
        public static List<Element> DeepQuerySelectorAll(this Element document, string selector, ShadowRootQueryMode shadowRootMode = ShadowRootQueryMode.Strict)
        {
            var splitOn = shadowRootMode == ShadowRootQueryMode.Wide ? " " : "::shadow";
            if (shadowRootMode == ShadowRootQueryMode.Wide)
            {
                selector = selector.Replace("::shadow", "");
            }
            var partials = selector.Split(splitOn, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var elems = document.QuerySelectorAll<Element>(partials[0]).Using(nodeList => nodeList.ToList());
            for (var i = 1; i < partials.Length; i++)
            {
                var partial = partials[i];
                var elemsInside = new List<Element>();
                for (var j = 0; j < elems.Count; j++)
                {
                    var el = elems[j];
                    using var shadow = el.ShadowRoot;
                    if (shadow != null)
                    {
                        var nodeList = shadow.QuerySelectorAll<Element>(partial).Using(nodeList => nodeList.ToList());
                        elemsInside.AddRange(nodeList);
                    }
                    if (shadowRootMode != ShadowRootQueryMode.Strict)
                    {
                        var nodeList = el.QuerySelectorAll<Element>(partial).Using(nodeList => nodeList.ToList());
                        elemsInside.AddRange(nodeList);
                    }
                }
                elems = elemsInside;
            }
            return elems;
        }
        public static List<Element> DeepQuerySelectorAll(this ShadowRoot document, string selector, ShadowRootQueryMode shadowRootMode = ShadowRootQueryMode.Strict)
        {
            var splitOn = shadowRootMode == ShadowRootQueryMode.Wide ? " " : "::shadow";
            if (shadowRootMode == ShadowRootQueryMode.Wide)
            {
                selector = selector.Replace("::shadow", "");
            }
            var partials = selector.Split(splitOn, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var elems = document.QuerySelectorAll<Element>(partials[0]).Using(nodeList => nodeList.ToList());
            for (var i = 1; i < partials.Length; i++)
            {
                var partial = partials[i];
                var elemsInside = new List<Element>();
                for (var j = 0; j < elems.Count; j++)
                {
                    var el = elems[j];
                    using var shadow = el.ShadowRoot;
                    if (shadow != null)
                    {
                        var nodeList = shadow.QuerySelectorAll<Element>(partial).Using(nodeList => nodeList.ToList());
                        elemsInside.AddRange(nodeList);
                    }
                    if (shadowRootMode != ShadowRootQueryMode.Strict)
                    {
                        var nodeList = el.QuerySelectorAll<Element>(partial).Using(nodeList => nodeList.ToList());
                        elemsInside.AddRange(nodeList);
                    }
                }
                elems = elemsInside;
            }
            return elems;
        }
    }
}
