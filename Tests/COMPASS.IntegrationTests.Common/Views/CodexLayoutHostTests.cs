using Autofac;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using COMPASS.Common.Domain;
using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Layouts;
using COMPASS.Common.Features.Tabs;
using COMPASS.Common.Shell;

namespace COMPASS.IntegrationTests.Common.Views
{
    [TestFixture]
    public class CodexLayoutHostTests
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

        /// <summary>
        /// Two tabs can show the same collection, and therefore the same codex view models. If they shared a view,
        /// the selection of one tab would show up in the other.
        /// </summary>
        [AvaloniaTest]
        public void ActiveLayout_SwitchToTabOfSameCollection_ShowsADifferentView()
        {
            CollectionTabVM firstTab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CollectionTabVM secondTab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost();

            layoutHost.ActiveLayout = firstTab.CurrentLayout;
            CodexLayoutView firstTabView = GetVisibleView(layoutHost);
            layoutHost.ActiveLayout = secondTab.CurrentLayout;
            CodexLayoutView secondTabView = GetVisibleView(layoutHost);

            Assert.Multiple(() =>
            {
                Assert.That(secondTabView, Is.Not.SameAs(firstTabView));
                Assert.That(secondTabView.DataContext, Is.SameAs(secondTab.CurrentLayout));
            });
        }

        /// <summary>
        /// Switching back to a tab must show the view it had before, so it keeps its selection and scroll position
        /// and doesn't have to be rebuilt.
        /// </summary>
        [AvaloniaTest]
        public void ActiveLayout_SwitchBackToPreviousTab_ReusesItsView()
        {
            CollectionTabVM firstTab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CollectionTabVM secondTab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost();

            layoutHost.ActiveLayout = firstTab.CurrentLayout;
            CodexLayoutView firstTabView = GetVisibleView(layoutHost);
            layoutHost.ActiveLayout = secondTab.CurrentLayout;
            layoutHost.ActiveLayout = firstTab.CurrentLayout;

            Assert.Multiple(() =>
            {
                Assert.That(GetVisibleView(layoutHost), Is.SameAs(firstTabView));
                Assert.That(layoutHost.Children, Has.Count.EqualTo(2));
            });
        }

        [AvaloniaTest]
        public void ChangeLayoutCommand_OnActiveTab_ReplacesTheOldView()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost();
            layoutHost.ActiveLayout = tab.CurrentLayout;
            CodexLayoutView oldView = GetVisibleView(layoutHost);

            tab.ChangeLayoutCommand.Execute(CodexLayout.List);
            layoutHost.ActiveLayout = tab.CurrentLayout;

            Assert.Multiple(() =>
            {
                Assert.That(layoutHost.Children, Has.Count.EqualTo(1));
                Assert.That(layoutHost.Children, Does.Not.Contain(oldView));
                Assert.That(GetVisibleView(layoutHost).DataContext, Is.SameAs(tab.CurrentLayout));
            });
        }

        [AvaloniaTest]
        public void ChangeLayoutCommand_ToCurrentLayout_KeepsTheView()
        {
            CollectionTabVM tab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost();
            LayoutViewModel originalLayout = tab.CurrentLayout;
            layoutHost.ActiveLayout = originalLayout;
            CodexLayoutView originalView = GetVisibleView(layoutHost);

            tab.ChangeLayoutCommand.Execute(originalLayout.LayoutType);
            layoutHost.ActiveLayout = tab.CurrentLayout;

            Assert.Multiple(() =>
            {
                Assert.That(tab.CurrentLayout, Is.SameAs(originalLayout));
                Assert.That(GetVisibleView(layoutHost), Is.SameAs(originalView));
            });
        }

        [AvaloniaTest]
        public void LayoutDispose_OfInactiveTab_RemovesItsView()
        {
            CollectionTabVM closedTab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CollectionTabVM remainingTab = _tabsViewModel.CreateTab(_collectionHandle.CollectionVM);
            CodexLayoutHost layoutHost = CreateShownHost();
            layoutHost.ActiveLayout = closedTab.CurrentLayout;
            CodexLayoutView closedTabView = GetVisibleView(layoutHost);
            layoutHost.ActiveLayout = remainingTab.CurrentLayout;

            closedTab.CurrentLayout.Dispose();

            Assert.Multiple(() =>
            {
                Assert.That(layoutHost.Children, Has.Count.EqualTo(1));
                Assert.That(layoutHost.Children, Does.Not.Contain(closedTabView));
            });
        }

        private static CodexLayoutHost CreateShownHost()
        {
            CodexLayoutHost layoutHost = new();
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
