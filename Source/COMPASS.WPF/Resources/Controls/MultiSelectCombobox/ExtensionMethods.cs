using COMPASS.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace COMPASS.Resources.Controls.MultiSelectCombobox
{
    internal static class ExtensionMethods
    {
        #region ListBox
        public static void ClearSelection(this ListBox suggestionList, Func<bool>? precondition = null)
        {
            if (precondition != null
                && !precondition())
            {
                return;
            }

            suggestionList?.SelectedItems.Clear();
        }
        public static void SelectNextItem(this ListBox suggestionList) => suggestionList.SingleItemSelection(1);
        public static void SelectPreviousItem(this ListBox suggestionList) => suggestionList.SingleItemSelection(-1);
        public static void SelectMultipleNextItem(this ListBox suggestionList) => suggestionList.SelectMultipleItem(1);
        public static void SelectMultiplePreviousItem(this ListBox suggestionList) => suggestionList.SelectMultipleItem(-1);
        public static int GetSelectionStart(this ListBox suggestionList)
        {
            if (suggestionList == null)
            {
                return -1;
            }

            return ListBoxAttachedProperties.GetSelectionStartIndex(suggestionList);
        }
        public static void SetSelectionStart(this ListBox suggestionList, int index)
        {
            if (suggestionList == null)
            {
                return;
            }

            ListBoxAttachedProperties.SetSelectionStartIndex(suggestionList, index);
        }
        public static int GetSelectionEnd(this ListBox suggestionList)
        {
            if (suggestionList == null)
            {
                return -1;
            }

            return ListBoxAttachedProperties.GetSelectionEndIndex(suggestionList);
        }
        public static void SetSelectionEnd(this ListBox suggestionList, int index)
        {
            if (suggestionList == null)
            {
                return;
            }

            ListBoxAttachedProperties.SetSelectionEndIndex(suggestionList, index);
        }

        public static void CleanOperation(this ListBox suggestionList, SuggestionCleanupOperation operation, IEnumerable goldenItemSource)
        {
            if ((operation & SuggestionCleanupOperation.ClearSelection) == SuggestionCleanupOperation.ClearSelection)
            {
                suggestionList.ClearSelection();
            }

            if ((operation & SuggestionCleanupOperation.ResetIndex) == SuggestionCleanupOperation.ResetIndex)
            {
                suggestionList.SetSelectionStart(-1);
                suggestionList.SetSelectionEnd(-1);
            }

            if ((operation & SuggestionCleanupOperation.ResetItemSource) == SuggestionCleanupOperation.ResetItemSource)
            {
                suggestionList.ItemsSource = goldenItemSource;
            }
        }

        private static void SingleItemSelection(this ListBox suggestionList, int delta)
        {
            if (suggestionList == null
                || (suggestionList.SelectedIndex + delta) < 0)
            {
                return;
            }

            suggestionList.SelectedIndex += delta;

            suggestionList.SetSelectionStart(-1);
            suggestionList.SetSelectionEnd(-1);
        }
        private static void SelectMultipleItem(this ListBox suggestionList, int delta)
        {
            if (suggestionList == null)
            {
                return;
            }

            ItemCollection suggestionItemSource = suggestionList.Items;
            int selectionStart = suggestionList.GetSelectionStart();
            int selectionEnd = suggestionList.GetSelectionEnd();
            int totalItemsCount = suggestionItemSource.Count;

            //If its first time - Start of selection
            if (selectionStart == -1 || selectionEnd == -1)
            {
                selectionStart = suggestionList.SelectedIndex;
                selectionEnd = suggestionList.SelectedIndex + delta;
                selectionEnd = (selectionEnd < 0)
                                ? 0
                                : (selectionEnd >= totalItemsCount)
                                ? totalItemsCount - 1
                                : selectionEnd;

                suggestionList.SetSelectionStart(selectionStart);
                suggestionList.SetSelectionEnd(selectionEnd);

                //Add current item to selected items list
                suggestionList.SelectedItems.Add(suggestionItemSource[selectionEnd]);
                return;
            }

            int newIndex = selectionEnd + delta;
            newIndex = (newIndex < 0)
                               ? 0
                               : (newIndex >= totalItemsCount)
                               ? totalItemsCount - 1
                               : newIndex;

            //If its boundary, return
            if (selectionEnd == newIndex)
            {
                return;
            }

            //If selection is shrinking then remove previous selected element
            if ((selectionStart > selectionEnd && newIndex > selectionEnd)
                || (selectionStart < selectionEnd && newIndex < selectionEnd))
            {
                suggestionList.SelectedItems.Remove(suggestionItemSource[selectionEnd]);
                suggestionList.SetSelectionEnd(newIndex);
                return;
            }

            //Otherwise, selection is growing, add current element to selected items list
            suggestionList.SetSelectionEnd(newIndex);
            suggestionList.SelectedItems.Add(suggestionItemSource[newIndex]);
        }

        [Flags]
        internal enum SuggestionCleanupOperation
        {
            None = 0,
            ResetIndex = 1,
            ClearSelection = 2,
            ResetItemSource = 4
        };
        #endregion

        #region RichTextBox
        public static string? GetCurrentText(this RichTextBox richTextBox) => GetCurrentRunBlock(richTextBox)?.Text;
        public static void ResetCurrentText(this RichTextBox richTextBox)
        {
            Run? runElement = GetCurrentRunBlock(richTextBox);
            if (runElement == null)
            {
                return;
            }

            runElement.Text = string.Empty;
        }
        public static void RemoveRunBlocks(this RichTextBox richTextBox)
        {
            Paragraph? paragraph = richTextBox?.GetParagraph();
            if (paragraph == null)
            {
                return;
            }

            var runTags = paragraph.Inlines.Where(r => r is Run).ToList();
            if (!runTags.Any())
            {
                return;
            }

            foreach (Inline t in runTags)
            {
                paragraph.Inlines.Remove(t);
            }
        }
        public static void TryFocus(this RichTextBox richTextBox)
        {
            try
            {
                if (richTextBox?.Focusable == true)
                {
                    richTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("RichTextBox could not be focussed", ex);
            }
        }
        public static void SetParagraphAsFirstBlock(this RichTextBox richTextBox)
        {
            try
            {
                if (richTextBox.GetParagraph() != null)
                {
                    return;
                }

                var paragraph = new Paragraph() { Style = new Style() };
                richTextBox.Document.Blocks.Clear();
                richTextBox.Document.Blocks.Add(paragraph);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to set paragraph as first block", ex);
            }
        }


        public static void AddToParagraph(this RichTextBox richTextBox, object itemToAdd, Func<object, Inline> createInlineElementFunc)
        {
            try
            {
                richTextBox?.SetParagraphAsFirstBlock();
                Paragraph? paragraph = richTextBox?.GetParagraph();
                if (paragraph == null)
                {
                    return;
                }

                Inline elementToAdd = createInlineElementFunc(itemToAdd);

                if (paragraph.Inlines.FirstInline == null)
                {
                    //First element to insert
                    paragraph.Inlines.Add(elementToAdd);
                    if (richTextBox is not null)
                    {
                        richTextBox.CaretPosition = richTextBox.CaretPosition.DocumentEnd;
                    }
                    return;
                }

                if (richTextBox?.CaretPosition.GetAdjacentElement(LogicalDirection.Forward) is Inline inlineToInsertBefore)
                {
                    paragraph.Inlines.InsertBefore(inlineToInsertBefore, elementToAdd);
                    richTextBox.CaretPosition = elementToAdd.ElementEnd;
                    return;
                }

                //Insert at the end
                paragraph.Inlines.InsertAfter(paragraph.Inlines.LastInline, elementToAdd);
                if (richTextBox is not null)
                {
                    richTextBox.CaretPosition = richTextBox.CaretPosition.DocumentEnd;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to add item to paragraph", ex);
            }
        }
        public static void ClearParagraph(this RichTextBox richTextBox)
        {
            richTextBox?.GetParagraph()?.Inlines.Clear();
            richTextBox?.SetParagraphAsFirstBlock();
        }

        public static bool DragDropAdjustSelection(this RichTextBox richTextBox, Point position)
        {
            TextPointer textPointer = richTextBox.GetPositionFromPoint(position, true);
            int endOffset = new TextRange(textPointer, richTextBox.Selection.End).Text.Length;
            int startOffset = new TextRange(textPointer, richTextBox.Selection.Start).Text.Length;
            if ((endOffset == 0 || startOffset == 0) && richTextBox.Selection.Text.Length > 0)
            {
                //if its on the same richTextBox and the drag and drop position is the same as actual, then we do not perform OnDragDrop.
                return false;
            }

            //Removal of the drag and drop element to be able to move it
            richTextBox.Selection.Text = "";
            richTextBox.CaretPosition = textPointer;
            richTextBox.Focus();
            return true;
        }
        public static DataObject? GetDragDropObject(this RichTextBox richTextBox)
        {
            var objectToSend = richTextBox.GetSelectedObjects();
            if ((objectToSend?.Length ?? 0) == 0)
            {
                return null;
            }

            DataObject data = new();
            data.SetData("Object", objectToSend);
            data.SetText(richTextBox.GetSelectedText());
            return data;
        }
        public static void SetSelectedTextToClipBoard(this RichTextBox richTextBox)
        {
            try
            {
                Clipboard.SetText(richTextBox.GetSelectedText());
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                //Failed to open Clipboard, this occur when user is copying fast multiples times data
                const uint CLIPBRD_E_CANT_OPEN = 0x800401D0;
                if ((uint)ex.ErrorCode != CLIPBRD_E_CANT_OPEN) throw;
            }
        }

        public static Run? GetCurrentRunBlock(this RichTextBox richTextBox) => richTextBox?.CaretPosition?.Parent as Run;
        public static Paragraph? GetParagraph(this RichTextBox richTextBox) => richTextBox?.Document?.Blocks.FirstBlock as Paragraph;
        public static object? GetNextItemTag(this RichTextBox richTextBox)
            => ((richTextBox.CaretPosition.GetAdjacentElement(LogicalDirection.Forward) as InlineUIContainer)?.Child as FrameworkElement)?.Tag;
        public static string GetSelectedText(this RichTextBox richTextBox)
        {
            string selectedText = string.Join(string.Empty,
                    richTextBox.GetParagraph()?.Inlines
                                .Where(inline => inline.ContentStart.CompareTo(richTextBox.Selection.Start) >= 0 && inline.ContentEnd.CompareTo(richTextBox.Selection.End) <= 0)
                                .Select(inline => inline.GetText()) ?? Enumerable.Empty<string>());

            //Dont forget to add CurrentText
            selectedText += richTextBox.Selection.Text.Trim();
            return selectedText;
        }

        public static object?[]? GetSelectedObjects(this RichTextBox richTextBox)
            => richTextBox?.GetParagraph()?.Inlines
                                .Where(inline => inline.ContentStart.CompareTo(richTextBox.Selection.Start) >= 0 && inline.ContentEnd.CompareTo(richTextBox.Selection.End) <= 0)
                                .Select(inline => inline.GetObject())
                                .Where(i => i != null).ToArray();

        public static TextBlock? GetTextBlock(this Inline inline)
        => (inline as InlineUIContainer)?.Child as TextBlock;
        public static object? GetObject(this Inline inline)
           => GetTextBlock(inline)?.Tag;
        public static string GetText(this Inline inline)
            => GetTextBlock(inline)?.Text ?? string.Empty;

        #endregion

        #region Popup
        public static void Show(this Popup popupElement, Func<bool> precondition, Action postAction) => ShowHide(popupElement, precondition, postAction, true);
        public static void Hide(this Popup popupElement, Func<bool> precondition, Action postAction) => ShowHide(popupElement, precondition, postAction, false);

        private static void ShowHide(this Popup popupElement, Func<bool> precondition, Action postAction, bool valueToSet)
        {
            try
            {
                if (popupElement == null
                    || popupElement.IsOpen == valueToSet)
                {
                    return;
                }

                bool proceed = precondition == null || precondition();
                if (!proceed)
                {
                    return;
                }

                popupElement.IsOpen = valueToSet;
                postAction?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Warn("Show or hide of popup failed", ex);
            }
        }

        #endregion

        #region Lookup Contract
        public static bool HasAnyExactMatch(this IEnumerable<object> source, string itemString, ILookUpContract contract, object sender)
            => source?.Any(i => contract?.IsItemEqualToString(sender, i, itemString) == true) == true;

        public static object? GetExactMatch(this IEnumerable<object> source, string itemString, ILookUpContract contract, object sender)
            => source?.FirstOrDefault(i => contract?.IsItemEqualToString(sender, i, itemString) == true);

        public static IEnumerable<object>? GetSuggestions(this IEnumerable<object> source, string itemString, ILookUpContract contract, object sender)
            => source?.Where(i => contract?.IsItemMatchingSearchString(sender, i, itemString) == true);
        #endregion
    }
}