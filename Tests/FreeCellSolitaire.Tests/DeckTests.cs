using System.Linq;
using FreeCellSolitaire.Core.CardModels;

namespace FreeCellSolitaire.Tests
{
    public class DeckTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Deck�o�P�򥻴���()
        {
            var deck = Deck.Create().Shuffle(101);
            Assert.AreEqual(deck.GetCardsCount(), 52);
            var card = deck.Pick();
            Assert.IsTrue(card != null);
            Assert.AreEqual(deck.GetCardsCount(), 51);
            deck.Reset();
            Assert.AreEqual(deck.GetCardsCount(), 52);

            var card2 = deck.Pick();
            Assert.AreEqual(card, card2);
        }

        [Test]
        public void PickTest�����`�����ӬO52�B������()
        {
            var deck = Deck.Create().Shuffle(101);
            List<Card> myCards = new List<Card>();
            Card card ;
            while (true)
            {
                card = deck.Pick();
                if (card == null)
                {
                    break;
                }
                myCards.Add(card);
            };
            Assert.AreEqual(myCards.Count, 52);
            Assert.AreEqual(myCards.Select(x => x).Distinct().Count(), 52);
        }

        [Test]
        public void ShuffleTest�T�w�P���~��()
        {
            var deck = Deck.Create().Shuffle(101);
            List<Card> myCards = new List<Card>();
            Card prevCard = null;
            Card card = null;
            int adjacentSumScore = 0;
            int adjacentCount = 0;
            while (true)
            {
                int baseScore = 3;
                prevCard = card;
                card = deck.Pick();
              
                //�o���F
                if (card == null)
                {
                    break;
                }

                if (prevCard != null)
                {

                    if (prevCard.Suit == card.Suit)
                    {
                        baseScore--;
                    }
                    if (Math.Abs(prevCard.Number - card.Number) <= 1)
                    {
                        baseScore= baseScore - 2;
                    }
                    adjacentSumScore += baseScore;
                    adjacentCount++;
                }
            };
            Assert.Greater((double)adjacentSumScore / adjacentCount, 2);
        }
    }
}