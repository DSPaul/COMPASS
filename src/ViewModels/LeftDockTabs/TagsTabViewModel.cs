using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Windows;

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
        public Tag ContextTag { get; set; }

        //Tag Creation ViewModel
        private IEditViewModel _addTagViewModel;
        public IEditViewModel AddTagViewModel
        {
            get => _addTagViewModel;
            set => SetProperty(ref _addTagViewModel, value);
        }

        //Add Tag Btns
        public ActionCommand AddTagCommand { get; private set; }
        public ActionCommand AddGroupCommand { get; private set; }
        public void AddTag() => AddTagViewModel = new TagEditViewModel(null, false);

        public void AddGroup() => AddTagViewModel = new TagEditViewModel(null, true);

        private RelayCommand<object[]> _addTagFilterCommand;
        public RelayCommand<object[]> AddTagFilterCommand => _addTagFilterCommand ??= new(AddTagFilterHelper);
        public void AddTagFilterHelper(object[] par)
        {
            Tag tag = (Tag)par[0];
            bool include = (bool)par[1];
            MVM.FilterVM.AddFilter(new(Filter.FilterType.Tag, tag), include); //needed because relaycommand only takes functions with one arg
        }

        #region Tag Context Menu
        public ActionCommand EditTagCommand { get; init; }
        public void EditTag()
        {
            if (ContextTag != null)
            {
                TagPropWindow tpw = new(new TagEditViewModel(ContextTag));
                tpw.ShowDialog();
                tpw.Topmost = true;
            }
        }

        public ActionCommand DeleteTagCommand { get; init; }
        public void DeleteTag()
        {
            //tag to delete is context, because DeleteTag is called from context menu
            if (ContextTag == null) return;
            MVM.CurrentCollection.DeleteTag(ContextTag);
            MVM.FilterVM.RemoveFilter(new(Filter.FilterType.Tag, ContextTag));

            //Go over all files and remove the tag from tag list
            foreach (var f in MVM.CurrentCollection.AllCodices)
            {
                f.Tags.Remove(ContextTag);
            }

            MVM.Refresh();
        }
        #endregion
    }
}
