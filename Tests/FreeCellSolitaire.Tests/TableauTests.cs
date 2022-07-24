using FreeCellSolitaire.Core.CardModels;
using FreeCellSolitaire.Core.GameModels;
using FreeCellSolitaire.Entities.GameEntities;

namespace FreeCellSolitaire.Tests
{
    public class TableauTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void _01_Init()
        {
            var deck = Deck.Create().Shuffle(101);
            var tableau = new Tableau(null);
            tableau.Init(deck);
            tableau.DebugInfo();
        }

        [Test]
        public void _02_GetCard()
        {
            var deck = Deck.Create().Shuffle(101);
            var tableau = new Tableau(null);
            tableau.Init(deck);            

            CardView card = tableau.GetCard(0);
            Console.WriteLine($"card:{card}");
            Assert.IsNotNull(card);
        }


        [Test]
        public void _03_Moveable()
        {
            var deck = Deck.Create(1).Shuffle(101);
            var tableau = new Tableau(null);
            var homecells = new Homecells(null);            
            tableau.Init(deck);

            //
            var card = tableau.GetColumn(0).GetLastCard();
            Assert.IsTrue(card.Moveable(tableau.GetColumn(1)));

            Assert.IsTrue(tableau.WasEmpty());
        }


        [Test]
        public void _04_MoveCard()
        {
            var deck = Deck.Create().Shuffle(101);
            var tableau = new Tableau(null);
            tableau.Init(deck);


            CardView card = tableau.GetCard(5);
            Assert.AreEqual(card.Suit, CardSuit.Heart);
            Assert.AreEqual(card.Number, 12);

            CardView card2 = tableau.GetCard(3);
            Assert.AreEqual(card2.Suit, CardSuit.Spade);
            Assert.AreEqual(card2.Number, 13);



            tableau.DebugColumnInfo(3);
            tableau.DebugColumnInfo(5);
            Assert.IsTrue(card.Move(tableau.GetColumn(3)));
            tableau.DebugColumnInfo(3);
            tableau.DebugColumnInfo(5);         
        }



        [Test]
        public void _04_MoveCardExpectFalse()
        {
            var deck = Deck.Create().Shuffle(101);
            var tableau = new Tableau(null);
            tableau.Init(deck);


            CardView card = tableau.GetCard(5);
            Assert.AreEqual(card.Suit, CardSuit.Heart);
            Assert.AreEqual(card.Number, 12);

            CardView card2 = tableau.GetCard(3);
            Assert.AreEqual(card2.Suit, CardSuit.Spade);
            Assert.AreEqual(card2.Number, 13);



            tableau.DebugColumnInfo(3);
            tableau.DebugColumnInfo(5);
            //TODO ����moveable
            Assert.IsFalse(card2.Move(tableau.GetColumn(5)));
            tableau.DebugColumnInfo(3);
            tableau.DebugColumnInfo(5);
        }

        [Test]
        public void _05_CheckEmpty()
        {
            var deck = Deck.Create(0).Shuffle(101);
            var tableau = new Tableau(null);
            tableau.Init(deck);
            Assert.IsTrue(tableau.WasEmpty());

            deck = Deck.Create(1).Shuffle(101);
            tableau = new Tableau(null);
            tableau.Init(deck);
            Assert.IsFalse(tableau.WasEmpty());

            
        }

    }
}