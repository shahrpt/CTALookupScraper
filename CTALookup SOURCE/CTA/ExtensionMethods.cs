using HtmlAgilityPack;

namespace CTALookup
{
    public static class ExtensionMethods
    {
        public static HtmlNode Sibling(this HtmlNode node, string name) {
            while (true) {
                node = node.NextSibling;
                if (node == null) {
                    return null;
                }
                if (node.Name.ToLower() != name.ToLower()) {
                    continue;
                }

                return node;
            }
        }
    }
}
