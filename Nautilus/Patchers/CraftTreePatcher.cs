namespace Nautilus.Patchers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Crafting;
using Nautilus.Handlers;
using Nautilus.Utility;

internal class CraftTreePatcher
{
    #region Internal Fields

    internal static Dictionary<CraftTree.Type, ModCraftTreeRoot> CustomTrees = new();
    internal static Dictionary<CraftTree.Type, List<Node>> NodesToRemove = new();
    internal static Dictionary<CraftTree.Type, List<CraftingNode>> CraftingNodes = new();
    internal static Dictionary<CraftTree.Type, List<TabNode>> TabNodes = new();

    #endregion

    private static Dictionary<CraftTree.Type, CraftTree> _originalTrees = new();

    #region Patches

    internal static void Patch(Harmony harmony)
    {
        harmony.PatchAll(typeof(CraftTreePatcher));
        InternalLogger.Log($"CraftTreePatcher is done.", LogLevel.Debug);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CraftTree), nameof(CraftTree.GetTree))]
    private static void GetTreePostfix(CraftTree.Type treeType, ref CraftTree __result)
    {
        var craftTree = !CustomTrees.TryGetValue(treeType, out var customRoot) ? __result : customRoot.CustomCraftingTree;

        if (craftTree == null)
        {
            // The game actually has a few CraftTree.Type that are not used...
            // None, Unused1, Unused2, etc...
            // but we still want to log just in case.
            InternalLogger.Debug($"Unable to find the CraftTree for type {treeType}.");
            return;
        }

        if (!_originalTrees.TryGetValue(treeType, out var originalTree))
        {
            originalTree = CopyTree(craftTree);
            _originalTrees.Add(treeType, originalTree);
        }

        var treeCopy = CopyTree(originalTree);
        
#if BELOWZERO
        if (treeType is CraftTree.Type.SeaTruckFabricator)
        {
            PatchCraftTree(ref treeCopy, CraftTree.Type.Fabricator);
        }
#endif
        PatchCraftTree(ref treeCopy, treeType);
        CraftTree.AddToCraftableTech(treeCopy);
        __result = treeCopy;
    }

    #endregion

    #region Handling Nodes

    private static void PatchCraftTree(ref CraftTree __result, CraftTree.Type type)
    {
        List<Node> removals = NodesToRemove.TryGetValue(type, out removals)? new List<Node>(removals): new List<Node>();
        RemoveNodes(ref __result, ref removals);

        AddCustomTabs(ref __result, type);
        PatchNodes(ref __result, type);

        // Remove any nodes added by mods that were marked for removal by other mods.
        RemoveNodes(ref __result, ref removals);
    }

    private static void AddCustomTabs(ref CraftTree tree, CraftTree.Type type)
    {
        List<TabNode> customTabs = TabNodes.TryGetValue(type, out customTabs) ? customTabs : new List<TabNode>();
        foreach (TabNode customNode in customTabs)
        {
            if(!TraverseTree(tree.nodes, customNode.Path, out var currentNode))
            {
                InternalLogger.Error($"Cannot add tab: {customNode.Name} to {customNode.Scheme} at {string.Join("/", customNode.Path)} as the parent node could not be found.");
                continue;
            }

            // Add the new tab node.
            currentNode.AddNode(new TreeNode[]
            {
                new CraftNode(customNode.Name, TreeAction.Expand, TechType.None)
            });
            InternalLogger.Debug($"Added tab: {customNode.Name} to {customNode.Scheme} at {string.Join("/", customNode.Path)}");
        }
    }

    private static void PatchNodes(ref CraftTree tree, CraftTree.Type type)
    {
        List<CraftingNode> customNodes = CraftingNodes.TryGetValue(type, out customNodes) ? customNodes : new List<CraftingNode>();
        foreach (var customNode in customNodes)
        {
            if (!TraverseTree(tree.nodes, customNode.Path, out var currentNode))
            {
                InternalLogger.Error($"Cannot add Crafting node: {customNode.TechType.AsString()} to {customNode.Scheme} at {string.Join("/", customNode.Path)} as the parent node could not be found.");
                continue;
            }

            // Add the node.
            currentNode.AddNode(new TreeNode[]
            {
                new CraftNode(customNode.TechType.AsString(false), TreeAction.Craft, customNode.TechType)
            });
            InternalLogger.Debug($"Added Crafting node: {customNode.TechType.AsString()} to {customNode.Scheme} at {string.Join("/", customNode.Path)}");
        }
    }

    /// <summary>
    /// This method can be used to both remove single child nodes, thus removing one recipe from the tree.
    /// Or it can remove entire tabs at once, removing the tab and all the recipes it contained in one go.
    /// </summary>
    private static void RemoveNodes(ref CraftTree tree, ref List<Node> nodesToRemove)
    {
        var safelist = new List<Node>(nodesToRemove).OrderByDescending(x => x.Path.Length); // Sort by path length so we remove the deepest nodes first.

        foreach (Node nodeToRemove in safelist)
        {
            if (nodeToRemove.Path == null || nodeToRemove.Path.Length == 0)
            {
                tree.nodes.root.Clear(); // Remove all child nodes (if any)
                nodesToRemove.Remove(nodeToRemove); // Remove the node from the list of nodes to remove
                InternalLogger.Debug($"Removed all nodes from {nodeToRemove.Scheme} tree.");
                continue;
            }

            // Get the names of each node in the path to traverse tree until we reach the node we want.
            if (!TraverseTree(tree.nodes, nodeToRemove.Path, out var currentNode))
            {
                InternalLogger.Warn($"Skipped removing craft tree node in {nameof(RemoveNodes)} for '{nodeToRemove.Scheme}' at '{string.Join("/", nodeToRemove.Path)}'. Could not find the node.");
                continue;
            }

            if (currentNode.parent == null) // should never happen but just in case...
            {
                InternalLogger.Warn($"Skipped removing craft tree node in {nameof(RemoveNodes)} for '{nodeToRemove.Scheme}' at '{string.Join("/", nodeToRemove.Path)}'. Could not identify the parent node.");
                continue;
            }

            if (!currentNode.parent.RemoveNode(currentNode))// Remove the node from its parent
            {
                InternalLogger.Warn($"Skipped removing craft tree node in {nameof(RemoveNodes)} for '{nodeToRemove.Scheme}' at '{string.Join("/", nodeToRemove.Path)}'. Could not remove the node.");
                continue;
            }
            currentNode.Clear(); // Remove all child nodes (if any)
            nodesToRemove.Remove(nodeToRemove); // Remove the node from the list of nodes to remove
            InternalLogger.Debug($"Removed node from {nodeToRemove.Scheme} tree at {string.Join("/", nodeToRemove.Path)}.");
        }
    }

    private static CraftTree CopyTree(CraftTree tree)
    {
        return new CraftTree(tree.id, (CraftNode)CopyCraftNode(tree.nodes));
    }
    
    /// <summary>
    /// Copy the specified node and it's inner nodes recursively.
    /// </summary>
    /// <param name="treeNode">The node to begin this operation on. Can be used on any node.</param>
    /// <returns>A complete copy of the passed node.</returns>
    private static TreeNode CopyCraftNode(TreeNode treeNode)
    {
        var copiedNode = treeNode.Copy();
        for (var i = 0; i < treeNode.nodes.Count; i++)
        {
            copiedNode.AddNode(CopyCraftNode(treeNode.nodes[i]));
        }        
        return copiedNode;
    }

    private static bool TraverseTree(TreeNode nodes, string[] path, out TreeNode currentNode)
    {
        currentNode = nodes;

        // Loop through the path provided by the node.
        // Get the node for the last path.
        for (int i = 0; i < path.Length; i++)
        {
            var currentPath = path[i];
            var lastnode = currentNode;
            var node2 = currentNode[currentPath];

            if (node2 != null)
            {
                currentNode = node2;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}