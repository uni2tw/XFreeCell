﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CoreForm.UI
{
    /// <summary>
    /// 左上暫存區
    /// </summary>
    public class TempZone : IZone
    {
        IGameForm form;
        public event ZoneHolderHandler HolderClick;
        public TempZone(IGameForm form)
        {
            this.form = form;
            this.Slots = new List<Slot>();
        }
        public bool CanSwap
        {
            get
            {
                return true;
            }
        }
        public bool CanMoveIn
        {
            get
            {
                return true;
            }
        }

        internal void RemoveCard(CardView selectedCard)
        {
            throw new NotImplementedException();
        }

        public bool CanMoveOut
        {
            get
            {
                return true;
            }
        }
        public int Capacity
        {
            get
            {
                return 1;
            }
        }

        public List<Slot> Slots { get; set; }
        public void Init(int cardWidth, int cardHeight, int right, int top)
        {
            for (int i = 0; i < 4; i++)
            {
                Control holder = new Panel
                {
                    Location = new Point(right, top),
                    Width = cardWidth,
                    Height = cardHeight,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackgroundImageLayout = ImageLayout.Stretch
                };

                form.SetControlReady(holder);
                var slot = new Slot(right, top, cardHeight, holder, Capacity, i, GameZoneType.Temp, this);
                Slots.Add(slot);
                holder.Click += delegate (object sender, EventArgs e)
                {
                    HolderClick?.Invoke(GameZoneType.Temp, slot);
                };
                right = right + cardWidth;
            }
        }
        public bool MoveCard(int slotIndex, CardView card)
        {
            var pSlot = card.Slot;
            pSlot.RemoveCard(card);
            return SetCard(slotIndex, card);
        }
        public bool SetCard(int x, CardView card)
        {
            //if (this.Slots[x].Cards.Count >= this.QueueLimit)
            if (this.Slots[x].IsFull)
            {
                return false;
            }

            Slot slot = this.Slots[x];

            slot.AddCard(card);
            card.View.Visible = true;
            card.View.Location = this.Slots[x].GetLocation(0);
            card.View.BringToFront();
            card.ZoneType = GameZoneType.Temp;
            card.Slot = slot;

            return true;
        }

        public bool IsAvailableFor(int x, CardView card)
        {
            if (this.Slots[x].IsFull)
            {
                return false;
            }
            CardView lastCard = this.Slots[x].LastCard();
            if (lastCard == null)
            {
                return true;
            }
            if (lastCard != null && lastCard.Suit == card.Suit && card.Number - lastCard.Number == 1)
            {
                return true;
            }
            return false;
        }

        public CardMoveAction TryAction(int slotIndex, out string message)
        {
            message = string.Empty;
            var srcSlotIndex = this.GetSlotSelectedIndex();
            if (srcSlotIndex == slotIndex)
            {
                this.DeselectSlots();
                return CardMoveAction.Deselect;
            }
            else if (this.Slots[slotIndex].IsFull)
            {
                return CardMoveAction.Fail;
            }
            return CardMoveAction.Move;
        }

        public void DeselectSlots()
        {
            foreach (var slot in this.Slots)
            {
                var lastCard = slot.LastCard();
                if (lastCard == null)
                {
                    continue;
                }
                lastCard.Actived = false;
            }
        }

        public int GetSlotSelectedIndex()
        {
            foreach (var slot in this.Slots)
            {
                var lastCard = slot.LastCard();
                if (lastCard == null)
                {
                    continue;
                }
                if (lastCard.Actived)
                {
                    return slot.Index;
                }
            }
            return -1;
        }

        public CardLocation GetSelectedInfo()
        {
            foreach (var slot in this.Slots)
            {
                var lastCard = slot.LastCard();
                if (lastCard == null)
                {
                    continue;
                }
                if (lastCard.Actived)
                {
                    return new CardLocation
                    {
                        ZoneType = GameZoneType.Waiting,
                        SlotIndex = slot.Index,
                        DataView = lastCard
                    };
                }
            }
            return null;
        }
    }
}