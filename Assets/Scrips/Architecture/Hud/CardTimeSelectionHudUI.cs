using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class CardTimeSelectionHudUI : MonoBehaviour
    {
        [Header("Sources")]
        [Tooltip("Wallet observed while Card Time selection is open.")]
        [SerializeField] private PlayerResourceWallet wallet;

        [Tooltip("Resource shown as the prominent Card Time energy value.")]
        [SerializeField] private ResourceDefinitionSO energyResource;

        [Tooltip("Slot caps and command labels used by this combat selection UI.")]
        [SerializeField] private CardTimeSelectionUiConfigSO selectionUiConfig;

        [Tooltip("Control scheme profile that supplies slot labels for the active selection scheme.")]
        [SerializeField] private CardTimeControlSchemeProfileSO controlSchemeProfile;

        [Tooltip("Presentation mapper for keyboard/gamepad slot labels.")]
        [SerializeField] private CardTimeInputDisplayMapperSO inputDisplayMapper;

        [SerializeField] private string selectedSchemeId;

        [Header("Root")]
        [Tooltip("Optional group used to show and hide selection UI without blocking gameplay.")]
        [SerializeField] private CanvasGroup rootGroup;

        [Tooltip("Optional non-blocking backdrop shown while Card Time selection is open.")]
        [SerializeField] private CanvasGroup backdropGroup;

        [SerializeField] private Color fallbackBackdropColor =
            new(r: 0f, g: 0f, b: 0f, a: 0.58f);

        [SerializeField] private Text categoryLabel;
        [SerializeField] private Text energyLabel;

        [Header("Cards")]
        [Tooltip("Card slot roots in presentation order. Neutral uses up to 8, Chain 6, Finisher 4.")]
        [SerializeField] private List<GameObject> cardSlots = new();

        [SerializeField] private List<CardTimeSelectionSlotUI> slotViews = new();
        [SerializeField] private List<Text> cardLabels = new();
        [SerializeField] private List<Graphic> cardFrames = new();

        [SerializeField] private Color normalCardColor =
            new(r: 0.12f, g: 0.22f, b: 0.25f, a: 0.88f);

        [SerializeField] private Color selectedCardColor =
            new(r: 0.05f, g: 0.82f, b: 0.95f, a: 1f);

        private CardTimeSelectionTransaction selection;
        private int previousSelectedIndex = -1;
        private int previousVisibleCount;

        private void Awake()
        {
            Hide();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Configure(
            PlayerResourceWallet resourceWallet,
            ResourceDefinitionSO energy,
            CardTimeSelectionUiConfigSO config,
            CardTimeControlSchemeProfileSO schemeProfile,
            string schemeId,
            CanvasGroup group,
            Text category,
            Text energyValue,
            IReadOnlyList<GameObject> slots,
            IReadOnlyList<CardTimeSelectionSlotUI> views,
            IReadOnlyList<Text> labels,
            IReadOnlyList<Graphic> frames)
        {
            Configure(
                resourceWallet,
                energy,
                config,
                schemeProfile,
                null,
                schemeId,
                group,
                null,
                category,
                energyValue,
                slots,
                views,
                labels,
                frames);
        }

        public void Configure(
            PlayerResourceWallet resourceWallet,
            ResourceDefinitionSO energy,
            CardTimeSelectionUiConfigSO config,
            CardTimeControlSchemeProfileSO schemeProfile,
            CardTimeInputDisplayMapperSO displayMapper,
            string schemeId,
            CanvasGroup group,
            CanvasGroup backdrop,
            Text category,
            Text energyValue,
            IReadOnlyList<GameObject> slots,
            IReadOnlyList<CardTimeSelectionSlotUI> views,
            IReadOnlyList<Text> labels,
            IReadOnlyList<Graphic> frames)
        {
            wallet = resourceWallet;
            energyResource = energy;
            selectionUiConfig = config;
            controlSchemeProfile = schemeProfile;
            inputDisplayMapper = displayMapper;
            selectedSchemeId = schemeId;
            rootGroup = group;
            backdropGroup = backdrop;
            categoryLabel = category;
            energyLabel = energyValue;
            cardSlots = slots != null ? new List<GameObject>(slots) : new List<GameObject>();
            slotViews = views != null
                ? new List<CardTimeSelectionSlotUI>(views)
                : new List<CardTimeSelectionSlotUI>();
            cardLabels = labels != null ? new List<Text>(labels) : new List<Text>();
            cardFrames = frames != null ? new List<Graphic>(frames) : new List<Graphic>();
        }

        public void BindSelection(CardTimeSelectionTransaction transaction)
        {
            selection = transaction;
            previousSelectedIndex = -1;
            previousVisibleCount = 0;
            Refresh();
        }

        public void SetControlScheme(
            CardTimeSelectionUiConfigSO config,
            CardTimeControlSchemeProfileSO schemeProfile,
            string schemeId)
        {
            selectionUiConfig = config;
            controlSchemeProfile = schemeProfile;
            selectedSchemeId = schemeId;
            Refresh();
        }

        public void ClearSelection(CardTimeSelectionTransaction transaction)
        {
            if (selection != transaction)
            {
                return;
            }

            selection = null;
            Hide();
        }

        public void ClearSelection()
        {
            selection = null;
            Hide();
        }

        public void PlaySlotAnimation(
            int slotIndex,
            CardTimeSelectionSlotAnimation animation)
        {
            if (slotIndex < 0 || slotIndex >= slotViews.Count || slotViews[slotIndex] == null)
            {
                return;
            }

            slotViews[slotIndex].PlayAnimation(animation);
        }

        private void Refresh()
        {
            if (selection == null || !selection.IsValid)
            {
                Hide();
                return;
            }

            var snapshot = selection.Current;
            var uiCapacity = selectionUiConfig != null
                ? selectionUiConfig.GetSlotCount(snapshot.Category)
                : GetPresentationCapacity(snapshot.Category);
            var scheme = controlSchemeProfile != null
                ? controlSchemeProfile.ResolveScheme(selectedSchemeId)
                : null;
            var schemeCapacity = scheme != null
                ? scheme.GetSlotCount(snapshot.Category)
                : uiCapacity;
            var capacity = Mathf.Min(uiCapacity, schemeCapacity);
            var visibleCount = Mathf.Min(snapshot.Candidates.Count, capacity);
            Show();

            if (categoryLabel != null)
            {
                categoryLabel.text = snapshot.Category.ToString();
            }

            if (energyLabel != null)
            {
                var current = wallet != null
                    ? wallet.GetCurrent(energyResource)
                    : 0f;
                var maximum = wallet != null
                    ? wallet.GetMaximum(energyResource)
                    : 0f;
                energyLabel.text =
                    $"ENERGY {HudValueMath.FormatWholeResource(current)} / {HudValueMath.FormatWholeResource(maximum)}";
            }

            var slotCount = Mathf.Max(
                Mathf.Max(cardSlots.Count, slotViews.Count),
                Mathf.Max(cardLabels.Count, cardFrames.Count));
            for (var index = 0; index < slotCount; index++)
            {
                var visible = index < visibleCount;
                SetSlotActive(index, visible);
                if (!visible)
                {
                    continue;
                }

                var card = snapshot.Candidates[index];
                var commandLabel = ResolveCommandLabel(scheme, snapshot.Category, index);
                BindSlot(index, card, commandLabel);
                SetLabel(index, card != null ? card.DisplayName : string.Empty);
                SetFrameColor(
                    index,
                    index == snapshot.SelectedIndex
                        ? selectedCardColor
                        : normalCardColor);
                SetSlotSelected(index, index == snapshot.SelectedIndex);
            }

            DispatchVisibilityAnimations(visibleCount);
            DispatchSelectionAnimations(snapshot.SelectedIndex);
            previousVisibleCount = visibleCount;
            previousSelectedIndex = snapshot.SelectedIndex;
        }

        private void Show()
        {
            EnsureBackdrop();
            if (rootGroup == null)
            {
                SetBackdropVisible(true);
                return;
            }

            rootGroup.alpha = 1f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
            SetBackdropVisible(true);
        }

        private void Hide()
        {
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.interactable = false;
                rootGroup.blocksRaycasts = false;
            }

            SetBackdropVisible(false);

            for (var index = 0; index < cardSlots.Count; index++)
            {
                if (cardSlots[index] != null)
                {
                    cardSlots[index].SetActive(false);
                }
            }

            for (var index = 0; index < previousVisibleCount; index++)
            {
                PlaySlotAnimation(index, CardTimeSelectionSlotAnimation.Hide);
            }

            previousSelectedIndex = -1;
            previousVisibleCount = 0;
        }

        private void EnsureBackdrop()
        {
            if (backdropGroup != null)
            {
                return;
            }

            var owner = new GameObject(
                "Card Selection Backdrop",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));
            var parent = transform.parent != null ? transform.parent : transform;
            owner.transform.SetParent(parent, worldPositionStays: false);
            owner.transform.SetSiblingIndex(transform.GetSiblingIndex());

            var rect = owner.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = owner.GetComponent<Image>();
            image.color = fallbackBackdropColor;
            image.raycastTarget = false;

            backdropGroup = owner.GetComponent<CanvasGroup>();
            SetBackdropVisible(false);
        }

        private void SetBackdropVisible(bool visible)
        {
            if (backdropGroup == null)
            {
                return;
            }

            backdropGroup.alpha = visible ? 1f : 0f;
            backdropGroup.interactable = false;
            backdropGroup.blocksRaycasts = false;
        }

        private void SetSlotActive(int index, bool active)
        {
            if (index >= cardSlots.Count || cardSlots[index] == null)
            {
                if (index < slotViews.Count && slotViews[index] != null)
                {
                    slotViews[index].SetVisible(active);
                }

                return;
            }

            cardSlots[index].SetActive(active);
            if (index < slotViews.Count && slotViews[index] != null)
            {
                slotViews[index].SetVisible(active);
            }
        }

        private void BindSlot(
            int index,
            CardDefinitionSO card,
            string commandLabel)
        {
            if (index >= slotViews.Count || slotViews[index] == null)
            {
                return;
            }

            slotViews[index].Bind(index, card, commandLabel);
        }

        private void SetSlotSelected(int index, bool selected)
        {
            if (index >= slotViews.Count || slotViews[index] == null)
            {
                return;
            }

            slotViews[index].SetSelected(selected);
        }

        private void SetLabel(int index, string text)
        {
            if (index >= cardLabels.Count || cardLabels[index] == null)
            {
                return;
            }

            cardLabels[index].text = text;
        }

        private void SetFrameColor(int index, Color color)
        {
            if (index >= cardFrames.Count || cardFrames[index] == null)
            {
                return;
            }

            cardFrames[index].color = color;
        }

        private string ResolveCommandLabel(
            CardTimeControlSchemeSO scheme,
            PlayerCardTimeState category,
            int slotIndex)
        {
            if (inputDisplayMapper != null)
            {
                return inputDisplayMapper.ResolveLabel(scheme, category, slotIndex);
            }

            return scheme != null
                ? scheme.GetDisplayLabel(category, slotIndex)
                : (slotIndex + 1).ToString();
        }

        private static int GetPresentationCapacity(PlayerCardTimeState category)
        {
            return category switch
            {
                PlayerCardTimeState.Neutral => 8,
                PlayerCardTimeState.Chain => 6,
                PlayerCardTimeState.Finisher => 4,
                _ => 0
            };
        }

        private void DispatchVisibilityAnimations(int visibleCount)
        {
            for (var index = previousVisibleCount; index < visibleCount; index++)
            {
                PlaySlotAnimation(index, CardTimeSelectionSlotAnimation.Show);
            }

            for (var index = visibleCount; index < previousVisibleCount; index++)
            {
                PlaySlotAnimation(index, CardTimeSelectionSlotAnimation.Hide);
            }
        }

        private void DispatchSelectionAnimations(int selectedIndex)
        {
            if (selectedIndex == previousSelectedIndex)
            {
                return;
            }

            PlaySlotAnimation(previousSelectedIndex, CardTimeSelectionSlotAnimation.Deselected);
            PlaySlotAnimation(selectedIndex, CardTimeSelectionSlotAnimation.Selected);
        }
    }
}
