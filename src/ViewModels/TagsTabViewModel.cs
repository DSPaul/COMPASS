using COMPASS.Models;
using COMPASS.ViewModels.Commands;

namespace COMPASS.ViewModels
{
    public class TagsTabViewModel : DealsWithTreeviews
    {
        public TagsTabViewModel() : base(MVM.CurrentCollection.RootTags)
        {
            EditTagCommand = new(EditTag);
            DeleteTagCommand = new(DeleteTag);
        }

        //Tag for Context Menu
        public Tag Context { get; set; }

        public ActionCommand EditTagCommand { get; init; }
        public void EditTag()
        {
            if (Context != null)
            {
                TagPropWindow tpw = new(new TagEditViewModel(Context));
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        public ActionCommand DeleteTagCommand { get; init; }
        public void DeleteTag()
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (Context == null) return;
            MVM.CurrentCollection.DeleteTag(Context);
            MVM.CollectionVM.RemoveTagFilter(Context);

            //Go over all files and remove the tag from tag list
            foreach (var f in MVM.CurrentCollection.AllCodices)
            {
                f.Tags.Remove(Context);
            }
            MVM.Refresh();
        }
    }
}
