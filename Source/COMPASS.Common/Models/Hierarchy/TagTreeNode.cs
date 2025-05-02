namespace COMPASS.Common.Models.Hierarchy;

//Compiled bindings don't like generics, so do this to help it
public class TagTreeNode : TreeNode<Tag>
{
    public TagTreeNode(Tag tag) : base(tag) { }
}

public class CheckableTagTreeNode : CheckableTreeNode<Tag>
{
    public CheckableTagTreeNode(Tag tag) : base(tag) { }
}