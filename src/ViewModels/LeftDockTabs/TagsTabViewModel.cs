using COMPASS.Models;
using COMPASS.ViewModels.Commands;

namespace COMPASS.ViewModels
{
    public class TagsTabViewModel : DealsWithTreeviews
    {
        public TagsTabViewModel() : base(MVM.CurrentCollection.RootTags)
        {
            AddTagCommand = new(AddTag);
            AddGroupCommand = new(AddGroup);
            EditTagCommand = new(EditTag);
            DeleteTagCommand = new(DeleteTag);
        }

        //Tag for Context Menu
        public Tag Context { get; set; }

        //Tag Creation ViewModel
        private IEditViewModel _addTagViewModel;
        public IEditViewModel AddTagViewModel
        {
            get { return _addTagViewModel; }
            set { SetProperty(ref _addTagViewModel, value); }
        }

        //Add Tag Btn
        public ActionCommand AddTagCommand { get; private set; }
        public ActionCommand AddGroupCommand { get; private set; }
        public void AddTag() => AddTagViewModel = new TagEditViewModel(null, false);
        public void AddGroup() => AddTagViewModel = new TagEditViewModel(null, true);

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
