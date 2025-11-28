using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Models.Hierarchy;

//Compiled bindings don't like generics, so do this to help it
public class TagTreeNode : TreeNode<TagViewModel>
{
    public TagTreeNode(TagViewModel tagVm) : base(tagVm) { }
}

public class CheckableTagTreeNode : CheckableTreeNode<TagViewModel>
{
    public CheckableTagTreeNode(TagViewModel tagVm) : base(tagVm) { }
}