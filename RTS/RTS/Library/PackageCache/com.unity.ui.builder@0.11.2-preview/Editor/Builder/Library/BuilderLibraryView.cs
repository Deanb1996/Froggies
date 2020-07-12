using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    abstract class BuilderLibraryView : VisualElement
    {
        VisualElement m_DocumentElement;
        BuilderSelection m_Selection;
        BuilderLibraryDragger m_Dragger;
        BuilderTooltipPreview m_TooltipPreview;
        BuilderPaneContent m_BuilderPaneContent;

        protected BuilderPaneWindow m_PaneWindow;

        public abstract VisualElement PrimaryFocusable { get; }

        public virtual void SetupView(BuilderLibraryDragger dragger, BuilderTooltipPreview tooltipPreview,
            BuilderPaneContent builderPaneContent, BuilderPaneWindow builderPaneWindow,
            VisualElement documentElement, BuilderSelection selection)
        {
            m_Dragger = dragger;
            m_TooltipPreview = tooltipPreview;
            m_BuilderPaneContent = builderPaneContent;
            m_PaneWindow = builderPaneWindow;
            m_DocumentElement = documentElement;
            m_Selection = selection;
        }

        public abstract void Refresh();

        protected void RegisterControlContainer(VisualElement element)
        {
            m_Dragger?.RegisterCallbacksOnTarget(element);

            if (m_TooltipPreview != null)
            {
                element.RegisterCallback<MouseEnterEvent>(OnItemMouseEnter);
                element.RegisterCallback<MouseLeaveEvent>(OnItemMouseLeave);
            }
        }

        protected void LinkToTreeViewItem(VisualElement element, BuilderLibraryTreeItem libraryTreeItem)
        {
            element.userData = libraryTreeItem;
            element.SetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName, libraryTreeItem);
        }

        protected BuilderLibraryTreeItem GetLibraryTreeItem(VisualElement element)
        {
            return (BuilderLibraryTreeItem) element.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName);
        }

        protected void AddItemToTheDocument(BuilderLibraryTreeItem item)
        {
            // If this is the uxml file entry of the currently open file, don't allow
            // the user to instantiate it (infinite recursion) or re-open it.
            if (item.Name == m_PaneWindow.document.uxmlFileName)
                return;

            var newElement = item.MakeVisualElementCallback?.Invoke();
            if (newElement == null)
                return;

            m_DocumentElement.Add(newElement);

            if (item.MakeElementAssetCallback == null)
                BuilderAssetUtilities.AddElementToAsset(m_PaneWindow.document, newElement);
            else
                BuilderAssetUtilities.AddElementToAsset(
                    m_PaneWindow.document, newElement, item.MakeElementAssetCallback);

            // TODO: ListView bug. Does not refresh selection pseudo states after a
            // call to Refresh().
            m_Selection.NotifyOfHierarchyChange();
            schedule.Execute(() =>
            {
                m_Selection.Select(null, newElement);
            }).ExecuteLater(200);
        }

        void OnItemMouseEnter(MouseEnterEvent evt)
        {
            var box = evt.target as VisualElement;
            var libraryTreeItem = box.GetProperty(BuilderConstants.LibraryItemLinkedManipulatorVEPropertyName) as BuilderLibraryTreeItem;

            if (!libraryTreeItem.HasPreview)
                return;

            var sample = libraryTreeItem.MakeVisualElementCallback?.Invoke();
            if (sample == null)
                return;

            m_TooltipPreview.Add(sample);
            m_TooltipPreview.Show();

            m_TooltipPreview.style.left = m_BuilderPaneContent.pane.resolvedStyle.width + BuilderConstants.TooltipPreviewYOffset;
            m_TooltipPreview.style.top = m_BuilderPaneContent.pane.resolvedStyle.top;
        }

        void OnItemMouseLeave(MouseLeaveEvent evt)
        {
            HidePreview();
        }

        protected void HidePreview()
        {
            m_TooltipPreview.Clear();
            m_TooltipPreview.Hide();
        }
    }
}
