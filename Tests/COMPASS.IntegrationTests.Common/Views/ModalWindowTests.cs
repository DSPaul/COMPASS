using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Platform;
using Avalonia.Threading;
using COMPASS.Infra.Avalonia.Modal;

namespace COMPASS.IntegrationTests.Common.Views
{
    [TestFixture]
    public class ModalWindowTests
    {
        [AvaloniaTest]
        public void ShowDialog_SmallContent_SizesToContent()
        {
            ModalWindow.Register<SmallContentViewModel, SmallContentView>();
            Window ownerWindow = CreateOwnerWindow();
            ModalWindow modalWindow = new(new SmallContentViewModel());

            _ = modalWindow.ShowDialog(ownerWindow);
            Dispatcher.UIThread.RunJobs();
            modalWindow.UpdateLayout();

            Assert.Multiple(() =>
            {
                Assert.That(modalWindow.ClientSize.Width, Is.EqualTo(SmallContentView.ContentWidth).Within(1));
                Assert.That(modalWindow.ClientSize.Height, Is.EqualTo(SmallContentView.ContentHeight).Within(1));
            });
        }

        [AvaloniaTest]
        public void ShowDialog_ContentTallerThanScreen_FitsOnScreen()
        {
            ModalWindow.Register<TallContentViewModel, TallContentView>();
            Window ownerWindow = CreateOwnerWindow();
            ModalWindow modalWindow = new(new TallContentViewModel());

            _ = modalWindow.ShowDialog(ownerWindow);
            Dispatcher.UIThread.RunJobs();
            modalWindow.UpdateLayout();

            Screen screen = modalWindow.Screens.Primary!;
            double screenHeight = screen.WorkingArea.Height / screen.Scaling;
            Assert.Multiple(() =>
            {
                Assert.That(modalWindow.ClientSize.Height, Is.LessThan(screenHeight),
                    "the title bar needs room on the screen as well");
                Assert.That(modalWindow.ClientSize.Width, Is.EqualTo(TallContentView.ContentWidth).Within(1),
                    "only the height should be limited, the width still fits the content");
            });
        }

        [AvaloniaTest]
        public void ShowDialog_ContentTallerThanScreen_KeepsFooterVisible()
        {
            ModalWindow.Register<TallContentViewModel, TallContentView>();
            Window ownerWindow = CreateOwnerWindow();
            ModalWindow modalWindow = new(new TallContentViewModel());

            _ = modalWindow.ShowDialog(ownerWindow);
            Dispatcher.UIThread.RunJobs();
            modalWindow.UpdateLayout();

            Control footer = ((TallContentView)GetModalView(modalWindow)).Footer;
            Point? footerBottom = footer.TranslatePoint(new Point(0, footer.Bounds.Height), modalWindow);
            Assert.That(footerBottom?.Y, Is.LessThanOrEqualTo(modalWindow.ClientSize.Height + 0.5));
        }

        [AvaloniaTest]
        public void Resize_LargerThanScreenAfterOpening_ContentFillsWindow()
        {
            ModalWindow.Register<StretchingContentViewModel, StretchingContentView>();
            Window ownerWindow = CreateOwnerWindow();
            ModalWindow modalWindow = new(new StretchingContentViewModel());
            _ = modalWindow.ShowDialog(ownerWindow);
            Dispatcher.UIThread.RunJobs();
            Screen screen = modalWindow.Screens.Primary!;
            Size largerThanScreen = new(screen.WorkingArea.Width / screen.Scaling + 500,
                screen.WorkingArea.Height / screen.Scaling + 500);

            modalWindow.Width = largerThanScreen.Width;
            modalWindow.Height = largerThanScreen.Height;
            Dispatcher.UIThread.RunJobs();
            modalWindow.UpdateLayout();

            Control modalView = GetModalView(modalWindow);
            Assert.Multiple(() =>
            {
                Assert.That(modalView.Bounds.Width, Is.EqualTo(largerThanScreen.Width).Within(1));
                Assert.That(modalView.Bounds.Height, Is.EqualTo(largerThanScreen.Height).Within(1));
            });
        }

        private static Window CreateOwnerWindow()
        {
            Window ownerWindow = new() { Width = 1000, Height = 800 };
            ownerWindow.Show();
            return ownerWindow;
        }

        private static Control GetModalView(ModalWindow modalWindow)
        {
            ContentControl contentPresenter = modalWindow.FindControl<ContentControl>("ContentPresenter")!;
            return (Control)contentPresenter.Presenter!.Child!;
        }

        private sealed class SmallContentViewModel : IModalViewModel
        {
            public string WindowTitle => "Small";
            public Action CloseAction { get; set; } = () => { };
        }

        private sealed class TallContentViewModel : IModalViewModel
        {
            public string WindowTitle => "Tall";
            public Action CloseAction { get; set; } = () => { };
        }

        private sealed class StretchingContentViewModel : IModalViewModel
        {
            public string WindowTitle => "Stretching";
            public Action CloseAction { get; set; } = () => { };
        }

        private sealed class StretchingContentView : UserControl
        {
            public StretchingContentView()
            {
                MinWidth = 300;
                MinHeight = 200;
                Content = new Border();
            }
        }

        private sealed class SmallContentView : UserControl
        {
            public const double ContentWidth = 300;
            public const double ContentHeight = 150;

            public SmallContentView()
            {
                Content = new Border { Width = ContentWidth, Height = ContentHeight };
            }
        }

        private sealed class TallContentView : UserControl
        {
            public const double ContentWidth = 400;

            public Control Footer { get; } = new Border { Height = 40 };

            public TallContentView()
            {
                MinWidth = 200;
                MinHeight = 200;
                Grid layoutGrid = new() { RowDefinitions = new RowDefinitions("*,auto"), Width = ContentWidth };
                layoutGrid.Children.Add(new ScrollViewer { Content = new Border { Height = 20000 } });
                Grid.SetRow(Footer, 1);
                layoutGrid.Children.Add(Footer);
                Content = layoutGrid;
            }
        }
    }
}
