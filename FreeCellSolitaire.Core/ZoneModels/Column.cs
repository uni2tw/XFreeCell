﻿using FreeCellSolitaire.Core.CardModels;
using FreeCellSolitaire.Core.Exceptions;
using FreeCellSolitaire.Entities.GameEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeCellSolitaire.Core.GameModels
{
    public class Column
    {
        public Column(IZone owner, int index, int capacity)
        {
            _owner = owner;
            _index = index;
            _capacity = capacity;
            _cards = new List<CardView>();
        }

        public bool Droppable(CardView cardView)
        {
            if (this._cards.Count + 1 > this._capacity)
            {
                return false;
            }
            return true;
        }

        public bool Draggable()
        {
            return this.Owner.CanMoveOut; 
        }

        public bool Empty()
        {
            return _cards.Count == 0;
        }

        public bool RemoveCard(CardView cardView)
        {
            return _cards.Remove(cardView);
        }

        private List<CardView> _cards;
        public bool AddCards(Card card)
        {            
            var cardView = new CardView(this, card);
            return AddCards(cardView);
        }
        public bool AddCards(CardView cardView)
        {
            if (_cards.Count >= _capacity)
            {
                return false;
            }            
            _cards.Add(cardView);
            return true;
        }

        public int GetCardsCount()
        {
            return _cards.Count;
        }

        public CardView GetCard(int cardIndex)
        {
            try
            {
                return _cards[cardIndex];
            }
            catch
            {
                throw new CardNotFoundException();
            }
        }
        public CardView GetLastCard()
        {
            return _cards.LastOrDefault();
        }
        public List<CardView> GetTableauLinkedCards(int maxNumber)
        {
            List<CardView> result = new List<CardView>();

            for (int i = 0; i < _cards.Count; i++)
            {
                var theCard = _cards[i];
                var nextCard = i == _cards.Count - 1 ? null : _cards[i + 1];
                if (nextCard == null)
                {
                    result.Add(theCard);
                }
                else if (nextCard.CheckLinkable(theCard, typeof(Tableau)) == false)
                {
                    result.Clear();
                }
                else
                {
                    result.Add(theCard);
                }
            }            
            if (result.Count > maxNumber) {
                result = result.Skip(result.Count - maxNumber).Take(maxNumber).ToList();
            }                        
            return result;
        }

        private IZone _owner;
        public IZone Owner
        {
            get
            {
                return _owner;
            }
        }

        private int _capacity;
        private int _index;

        public int Index
        {
            get
            {
                return _index;
            }
        }

        public string Code
        {
            get
            {
                return this.Owner.GetType().Name.ToLower().Substring(0, 1) + Index.ToString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in _cards)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append($"{card}");
            }
            return sb.ToString();
        }
        public string ToNotation()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var card in _cards)
            {
                if (sb.Length > 0)
                {
                    sb.Append(",");
                }
                sb.Append($"{card.ToNotation()}");
            }
            return sb.ToString();
        }


        //AddCards("s1,s2,s3");
        public void AddCards(string cardExpressions)
        {
            if (string.IsNullOrEmpty(cardExpressions))
            {
                return;
            }
            string[] exprs = cardExpressions.Split(',');
            foreach(var expr in exprs)
            {
                Card card = new Card();                    
                if (expr[0] == 's')
                {
                    card.Suit = CardSuit.Spade;
                    card.Number = Convert.ToInt32(expr.Substring(1));
                } 
                else if (expr[0] == 'h')
                {
                    card.Suit = CardSuit.Heart;
                    card.Number = Convert.ToInt32(expr.Substring(1));
                }
                else if (expr[0] == 'd')
                {
                    card.Suit = CardSuit.Diamond;
                    card.Number = Convert.ToInt32(expr.Substring(1));
                }
                else if (expr[0] == 'c')
                {
                    card.Suit = CardSuit.Club;
                    card.Number = Convert.ToInt32(expr.Substring(1));
                }
                this.AddCards(card);
            }
        }        
    }
}
