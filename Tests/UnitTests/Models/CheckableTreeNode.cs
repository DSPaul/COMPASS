using COMPASS.Models;
using COMPASS.Tools;
using System.Collections.ObjectModel;

namespace Tests.UnitTests.Models
{
    [TestClass]
    public class CheckableTreeNode
    {
        private static CheckableTreeNode<Tag>? checkableRoot;

        [ClassInitialize]
        public static void Initialize(TestContext context)
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

        [TestMethod]
        public void TestDownwardsPropagatiion()
        {
            //Downwards false
            checkableRoot!.IsChecked = false;
            Assert.IsTrue(checkableRoot!.Children.Flatten().All(child => child.IsChecked == false));

            //Downwards true
            checkableRoot!.IsChecked = true;
            Assert.IsTrue(checkableRoot!.Children.Flatten().All(child => child.IsChecked == true));
        }

        [TestMethod]
        public void TestUpwardsPropagatiion()
        {
            //uncheck all to start
            checkableRoot!.IsChecked = false;

            var firstChild = checkableRoot.Children[0];
            var secondChild = checkableRoot.Children[1];

            //check one, all above should become null
            firstChild.Children.First().IsChecked = true;
            Assert.IsNull(firstChild.IsChecked);
            Assert.IsNull(checkableRoot.IsChecked);

            //check the other, should cause parent to be checked
            firstChild.Children[1].IsChecked = true;
            Assert.IsTrue(firstChild.IsChecked);
            Assert.IsNull(checkableRoot.IsChecked);

            //now uncheck children of first, because it is containerOnly, should be unchecked
            firstChild.Children[0].IsChecked = false;
            firstChild.Children[1].IsChecked = false;
            Assert.IsTrue(firstChild.ContainerOnly);
            Assert.IsFalse(firstChild.IsChecked);
            Assert.IsFalse(checkableRoot.IsChecked);

            //check second child and uncheck children, because it is NOT containerOnly, should stay checked
            secondChild.IsChecked = true;
            secondChild.Children[0].IsChecked = false;
            secondChild.Children[1].IsChecked = false;
            Assert.IsFalse(secondChild.ContainerOnly);
            Assert.IsTrue(secondChild.IsChecked);
            Assert.IsNull(checkableRoot.IsChecked);
        }
    }
}
