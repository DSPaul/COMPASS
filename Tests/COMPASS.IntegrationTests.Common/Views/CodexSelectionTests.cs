using Autofac;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using COMPASS.Common.Domain;
using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Common.Features.Layouts;
using COMPASS.Common.Features.Tabs;
using COMPASS.Common.Shell;

namespace COMPASS.IntegrationTests.Common.Views
{
    [TestFixture]
    public class CodexSelectionTests
    {
        private IContainer _container = null!;
        private CollectionHandle _collectionHandle = null!;
        private TabsViewModel _tabsViewModel = null!;

        [SetUp]
        public void SetUp()
        {
            ViewRegistrar.RegisterLayouts();
            _container = Helpers.SetupContainer(_ => { });
            _collectionHandle = _container.Resolve<CollectionManager>().GetOrCreateInitialCollectionVM();
            _tabsViewModel = _container.Resolve<TabsViewModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _collectionHandle.Dispose();
            _container.Dispose();
        }

        [AvaloniaTest]
        public void SelectCodices_WithCodexOfTheCollection_SelectsItInTheCardLayout()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM, layout: CodexLayout.Card);
            Codex codex = AddCodex(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost(tab.CurrentLayout);

            tab.SelectCodices([codex]);
            Dispatcher.UIThread.RunJobs();

            CodexViewModel codexVm = GetCodexVm(tab, codex);
            ListBox cardListBox = GetVisibleView(layoutHost).GetVisualDescendants().OfType<ListBox>().Single();
            Assert.Multiple(() =>
            {
                Assert.That(cardListBox.SelectedItems, Does.Contain(codexVm));
                Assert.That(tab.CurrentLayout.SelectedCodices, Does.Contain(codexVm));
                Assert.That(cardListBox.IsKeyboardFocusWithin, Is.True, "shortcuts should act on the new selection");
            });
        }

        [AvaloniaTest]
        public void SelectCodices_WithCodexOfTheCollection_SelectsItInTheListLayout()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM, layout: CodexLayout.List);
            Codex codex = AddCodex(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost(tab.CurrentLayout);

            tab.SelectCodices([codex]);
            Dispatcher.UIThread.RunJobs();

            CodexViewModel codexVm = GetCodexVm(tab, codex);
            DataGrid layoutDataGrid = GetVisibleView(layoutHost).GetVisualDescendants().OfType<DataGrid>().Single();
            Assert.Multiple(() =>
            {
                Assert.That(layoutDataGrid.SelectedItems, Does.Contain(codexVm));
                Assert.That(layoutDataGrid.IsKeyboardFocusWithin, Is.True, "shortcuts should act on the new selection");
            });
        }

        [AvaloniaTest]
        public void SelectCodices_ReplacingAPreviousSelection_DeselectsTheEarlierCodex()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM, layout: CodexLayout.Card);
            Codex firstCodex = AddCodex(_collectionHandle.CollectionVM);
            Codex secondCodex = AddCodex(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost(tab.CurrentLayout);
            ListBox cardListBox = GetVisibleView(layoutHost).GetVisualDescendants().OfType<ListBox>().Single();

            tab.SelectCodices([firstCodex]);
            Dispatcher.UIThread.RunJobs();
            tab.SelectCodices([secondCodex]);
            Dispatcher.UIThread.RunJobs();

            Assert.Multiple(() =>
            {
                Assert.That(cardListBox.SelectedItems, Does.Not.Contain(GetCodexVm(tab, firstCodex)));
                Assert.That(cardListBox.SelectedItems, Does.Contain(GetCodexVm(tab, secondCodex)));
            });
        }

        [AvaloniaTest]
        public void SelectCodices_WithCodexOfAnotherCollection_SelectsNothing()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM, layout: CodexLayout.Card);
            CodexLayoutHost layoutHost = CreateShownHost(tab.CurrentLayout);
            Codex codexInOtherCollection = new(new CodexCollection("Another Collection"));

            tab.SelectCodices([codexInOtherCollection]);
            Dispatcher.UIThread.RunJobs();

            ListBox cardListBox = GetVisibleView(layoutHost).GetVisualDescendants().OfType<ListBox>().Single();
            Assert.That(cardListBox.SelectedItems, Is.Empty);
        }

        [AvaloniaTest]
        public void SelectCodices_WithCodexThatTheFiltersHide_SelectsNothing()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM, layout: CodexLayout.Card);
            Codex visibleCodex = AddCodex(_collectionHandle.CollectionVM);
            visibleCodex.Title = "Visible item";
            CodexLayoutHost layoutHost = CreateShownHost(tab.CurrentLayout);
            ListBox cardListBox = GetVisibleView(layoutHost).GetVisualDescendants().OfType<ListBox>().Single();

            tab.FiltersVM.SearchCommand.Execute("Nothing matches this");
            Dispatcher.UIThread.RunJobs();

            tab.SelectCodices([visibleCodex]);
            Dispatcher.UIThread.RunJobs();

            Assert.That(cardListBox.SelectedItems, Is.Empty);
        }

        [AvaloniaTest]
        public void SelectCodices_WithHomeLayout_SelectsItInEveryListThatShowsIt()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM, layout: CodexLayout.Home);
            Codex codex = AddCodex(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost(tab.CurrentLayout);
            Dispatcher.UIThread.RunJobs();

            tab.SelectCodices([codex]);
            Dispatcher.UIThread.RunJobs();

            CodexViewModel codexVm = GetCodexVm(tab, codex);
            List<ListBox> homeListBoxes = [.. GetVisibleView(layoutHost).GetVisualDescendants().OfType<ListBox>()];
            Assert.Multiple(() =>
            {
                Assert.That(homeListBoxes, Is.Not.Empty);
                foreach (ListBox homeListBox in homeListBoxes)
                {
                    bool listShowsCodex = homeListBox.ItemsSource?.OfType<CodexViewModel>().Contains(codexVm) == true;
                    Assert.That(homeListBox.SelectedItems, listShowsCodex ? Does.Contain(codexVm) : Is.Empty,
                        "every list that shows the codex should have it selected");
                }
                Assert.That(tab.CurrentLayout.SelectedCodices, Does.Contain(codexVm));
            });
        }

        private static CodexViewModel GetCodexVm(CollectionTabVM tab, Codex codex)
            => tab.CollectionVM.AllCodexVms.Single(codexVm => codexVm.GetModel() == codex);

        private static Codex AddCodex(CodexCollectionVM collectionVm)
        {
            Codex codex = new(collectionVm.Collection);
            collectionVm.Collection.AllCodices.Add(codex);
            return codex;
        }

        private static CodexLayoutHost CreateShownHost(LayoutViewModel activeLayout)
        {
            CodexLayoutHost layoutHost = new() { ActiveLayout = activeLayout };
            Window window = new() { Content = layoutHost, Width = 800, Height = 600 };
            window.Show();
            return layoutHost;
        }

        private static CodexLayoutView GetVisibleView(CodexLayoutHost layoutHost)
        {
            return layoutHost.Children.OfType<CodexLayoutView>().Single(layoutView => layoutView.IsVisible);
        }
    }
}
