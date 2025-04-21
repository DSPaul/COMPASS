using System.Collections.ObjectModel;
using COMPASS.Common.Models;
using COMPASS.Common.Tools;

namespace Tests.UnitTests.Models
{
    public class CheckableTreeNode
    {
        private static CheckableTreeNode<Tag>? checkableRoot;

        [OneTimeSetUp]
        public void Initialize()
        {
            //Setup
            Tag root = new()
            {
                ID = 1,
                IsGroup = true,
                Children = new ObservableCollection<Tag>()
                {
                    new() //L1 that is a group
                    {
                        ID = 2,
                        IsGroup = true,
                        Children = new ObservableCollection<Tag>()
                        {
                            new() //some L2 children
                            {
                                ID = 3,
                                IsGroup = false,
                            },
                            new() //some L2 children
                            {
                                ID = 4,
                                IsGroup = false,
                            },
                        }
                    },
                    new() //L1 that is not a group
                    {
                        ID = 5,
                        IsGroup = false,
                        Children = new ObservableCollection<Tag>()
                        {
                            new() //some L2 children
                            {
                                ID = 6,
                                IsGroup = false,
                            },
                            new() //some L2 children
                            {
                                ID = 7,
                                IsGroup = false,
                            },
                        }
                    },
                }
            };

            checkableRoot = new(root, containerOnly: root.IsGroup);
            foreach (var item in checkableRoot.Children.Flatten())
            {
                item.ContainerOnly = item.Item.IsGroup;
            }
        }

        [Test]
        public void TestDownwardsPropagation()
        {
            //Downwards false
            checkableRoot!.IsChecked = false;
            Assert.That(checkableRoot!.Children.Flatten().All(child => child.IsChecked == false));

            //Downwards true
            checkableRoot!.IsChecked = true;
            Assert.That(checkableRoot!.Children.Flatten().All(child => child.IsChecked == true));
        }

        [Test]
        public void TestUpwardsPropagatiion()
        {
            //uncheck all to start
            checkableRoot!.IsChecked = false;

            var firstChild = checkableRoot.Children[0];
            var secondChild = checkableRoot.Children[1];

            //check one, all above should become null
            firstChild.Children.First().IsChecked = true;
            Assert.Multiple(() =>
            {
                Assert.That(firstChild.IsChecked, Is.Null);
                Assert.That(checkableRoot.IsChecked, Is.Null);
            });

            //check the other, this should cause the parent to be checked
            firstChild.Children[1].IsChecked = true;
            Assert.Multiple(() =>
            {
                Assert.That(firstChild.IsChecked, Is.True);
                Assert.That(checkableRoot.IsChecked, Is.Null);
            });

            //now uncheck children of first, because it is containerOnly, should be unchecked
            firstChild.Children[0].IsChecked = false;
            firstChild.Children[1].IsChecked = false;
            Assert.Multiple(() =>
            {
                Assert.That(firstChild.ContainerOnly);
                Assert.That(firstChild.IsChecked, Is.False);
                Assert.That(checkableRoot.IsChecked, Is.False);
            });

            //check second child and uncheck children, because it is NOT containerOnly, should stay checked
            secondChild.IsChecked = true;
            secondChild.Children[0].IsChecked = false;
            secondChild.Children[1].IsChecked = false;
            Assert.Multiple(() =>
            {
                Assert.That(secondChild.ContainerOnly, Is.False);
                Assert.That(secondChild.IsChecked, Is.True);
                Assert.That(checkableRoot.IsChecked, Is.Null);
            });
        }
    }
}
