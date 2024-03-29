﻿using FreeCellSolitaire.Core.CardModels;
using FreeCellSolitaire.Entities.GameEntities;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeCellSolitaire.Core.GameModels;

public class Game : IGame
{
    public Homecells Homecells { get; set; }

    public Foundations Foundations { get; set; }

    public Tableau Tableau { get; set; }
    public Deck Deck { get; private set; }

    public bool EnableAssist { get; set; }
    public List<Track> Tracks { get; set; }

    public Game()
    {
        Deck = Deck.Create();
        Tracks = new List<Track>();
    }

    public int GetExtraMobility(Column destColumn)
    {
        Debug.Assert(destColumn.Owner is Tableau);

        int result = 1;
        for (int i = 0; i < Tableau.ColumnCount; i++)
        {
            if (Tableau.GetColumn(i).Empty())
            {
                result++;
            }
        }
        for (int i = 0; i < Foundations.ColumnCount; i++)
        {
            if (Foundations.GetColumn(i).Empty())
            {
                result++;
            }
        }
        //移到空的column，那可騰挪用的column會少1
        if (destColumn.GetCardsCount() == 0)
        {
            result = result - 1;
        }
        return result;
    }

    Regex regNotation = new Regex(@"([ft])(\d{1,2})([fth])(\d{1,2})", RegexOptions.Singleline | RegexOptions.Compiled);
    public bool Move(string notation)
    {
        Match match = regNotation.Match(notation);

        string srcZone = match.Groups[1].Value;
        string destZone = match.Groups[3].Value;
        int srcColumn = int.Parse(match.Groups[2].Value);
        int destColumn = int.Parse(match.Groups[4].Value);

        CardView card = null;
        if (srcZone == "t" && destZone == "t")
        {
            int mobility = this.GetExtraMobility(Tableau.GetColumn(destColumn));

            var cards = this.Tableau.GetColumn(srcColumn).GetTableauLinkedCards(mobility);
            var destCard = Tableau.GetColumn(destColumn).GetLastCard();
            if (destCard == null)
            {
                card = cards.FirstOrDefault();
            }
            else
            {
                card = cards.FirstOrDefault(x => x.CheckLinkable(destCard, typeof(Tableau)));
            }
        }
        else if (srcZone == "t")
        {
            card = this.Tableau.GetColumn(srcColumn).GetLastCard();
        }
        else if (srcZone == "f")
        {
            card = this.Foundations.GetColumn(srcColumn).GetLastCard();
        }
        if (card == null)
        {
            return false;
        }
        bool result = true;
        if (destZone == "t")
        {
            result = card.Move(Tableau.GetColumn(destColumn));
        }
        else if (destZone == "f")
        {
            result = card.Move(Foundations.GetColumn(destColumn));
        }
        else if (destZone == "h")
        {
            result = card.Move(Homecells.GetColumn(destColumn));
        }

        if (EnableAssist)
        {
            TryAssistMove();
        }

        this.Tracks.Add(new Track(notation));
        Console.WriteLine($"{this.Tracks.Count}.{notation}");

        return result;
    }
    public void TryAssistMove()
    {
        bool anything;
        do
        {
            anything = false;


            Func<CardView, bool> TryMoveToHomecells = delegate (CardView theCard)
            {
                for (int i = 0; i < this.Homecells.ColumnCount; i++)
                {
                    if (theCard.Moveable(Homecells.GetColumn(i)) && theCard.NeededByOthers(this.Tableau, this.Foundations) == false)
                    {
                        if (theCard.Move(Homecells.GetColumn(i)))
                        {
                            return true;
                        }
                    }
                }
                return false;
            };

            for (int i = 0; i < this.Foundations.ColumnCount; i++)
            {
                var srcCard = this.Foundations.GetColumn(i).GetLastCard();
                if (srcCard != null)
                {
                    if (TryMoveToHomecells(srcCard))
                    {
                        anything = true;
                        break;
                    }
                }
            }
            if (anything == false)
            {
                for (int i = 0; i < this.Tableau.ColumnCount; i++)
                {
                    var srcCard = this.Tableau.GetColumn(i).GetLastCard();
                    if (srcCard != null)
                    {
                        if (TryMoveToHomecells(srcCard))
                        {
                            anything = true;
                            break;
                        }
                    }
                }
            }
        } while (anything);
    }

    public void DebugInfo(int stepNum, bool enabled = true)
    {
        Console.Write(GetDebugInfo(stepNum.ToString(), enabled));
    }
    public void DebugInfo(string stepNum = "", bool enabled = true)
    {
        Console.Write(GetDebugInfo(stepNum, enabled));
    }

    public string GetDebugInfo(string stepNum = "", bool enabled = true)
    {
        if (enabled == false)
        {
            return string.Empty;
        }
        StringBuilder sb = new StringBuilder();
        if (string.IsNullOrEmpty(stepNum) == false)
        {
            sb.AppendLine($"=== {stepNum} ===");
        }
        if (this.Tableau != null)
        {
            sb.Append(this.Tableau.GetDebugInfo(false));
        }
        if (this.Foundations != null)
        {
            sb.Append(this.Foundations.GetDebugInfo(false));
        }
        if (this.Homecells != null)
        {
            sb.Append(this.Homecells.GetDebugInfo());
        }
        return sb.ToString();
    }




    /// <summary>
    /// 調整成表示 WasStarted ，就算完成也會回傳  true
    /// </summary>
    /// <returns></returns>
    public bool IsPlaying()
    {
        if (Tableau == null || Homecells == null)
        {
            return false;
        }
        int unfinish = 0;
        int finish = 0;
        for (int i = 0; i < Tableau.ColumnCount; i++)
        {
            unfinish += Tableau.GetColumn(i).GetCardsCount();
        }
        for (int i = 0; i < Homecells.ColumnCount; i++)
        {
            finish += Homecells.GetColumn(i).GetCardsCount();
        }
        return unfinish + finish > 0;
    }

    public bool IsCompleted()
    {
        if (Homecells.GetColumn(0).GetCardsCount() +
            Homecells.GetColumn(1).GetCardsCount() +
            Homecells.GetColumn(2).GetCardsCount() +
            Homecells.GetColumn(3).GetCardsCount() == 52)
        {
            return true;
        }
        return false;
    }

    public IGame Clone()
    {
        var clone = new Game { EnableAssist = this.EnableAssist };
        clone.Tableau = this.Tableau.Clone() as Tableau;
        clone.Homecells = this.Homecells.Clone() as Homecells;
        clone.Foundations = this.Foundations.Clone() as Foundations;
        clone.Tracks = this.Tracks.Select(x => x.Clone()).ToList();
        return clone;
    }

    public GameStatus EstimateGameover(bool debug = false)
    {
        if (IsCompleted())
        {
            return GameStatus.Completed;
        }
        Queue<IGame> queueItems = new Queue<IGame>();
        HashSet<IGame> samples = new HashSet<IGame>();
        queueItems.Enqueue(this.Clone());
        int depth = 0;
        while (queueItems.Count > 0 && depth <= 2)
        {
            var data = GetPossibleSituations(queueItems.Dequeue(), ref depth);

            foreach (var datum in data)
            {
                if (samples.Contains(datum) == false)
                {
                    queueItems.Enqueue(datum);
                    samples.Add(datum);
                    datum.DebugInfo($"s-{samples.Count}", debug);
                }
            }

            if (samples.Count > 2)
            {
                return GameStatus.Playable;
            }
        };

        if (samples.Count == 0)
        {
            return GameStatus.DeadEnd;
        }
        else if (samples.Count <= 2)
        {
            return GameStatus.Checkmate;
        }
        return GameStatus.Playable;
    }

    public List<IGame> GetPossibleSituations(IGame game, ref int depth)
    {
        depth++;
        List<IGame> samples = new List<IGame>();
        for (int i = 0; i < game.Tableau.ColumnCount + game.Foundations.ColumnCount; i++)
        {
            for (int j = 0; j < game.Tableau.ColumnCount + game.Foundations.ColumnCount + game.Homecells.ColumnCount; j++)
            {
                var clone = game.Clone();

                Column srcColumn;
                if (i < clone.Tableau.ColumnCount)
                {
                    srcColumn = clone.Tableau.GetColumn(i);
                }
                else
                {
                    srcColumn = clone.Foundations.GetColumn(i - clone.Tableau.ColumnCount);
                }

                var srcCard = srcColumn.GetLastCard();
                if (srcCard == null)
                {
                    continue;
                }
                Column destColumn;
                if (j < clone.Tableau.ColumnCount)
                {
                    destColumn = clone.Tableau.GetColumn(j);
                }
                else if (j < game.Tableau.ColumnCount + game.Foundations.ColumnCount)
                {
                    destColumn = clone.Foundations.GetColumn(j - clone.Tableau.ColumnCount);
                }
                else
                {
                    destColumn = clone.Homecells.GetColumn(j - game.Tableau.ColumnCount - game.Foundations.ColumnCount);
                }

                if (srcCard.Move(destColumn))
                {
                    if (EnableAssist)
                    {
                        clone.TryAssistMove();
                    }
                    samples.Add(clone);
                    continue;
                }
            }
        }
        return samples;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }
        if (this == obj)
        {
            return true;
        }
        var castObj = obj as IGame;
        if (castObj == null)
        {
            return false;
        }
        return castObj.GetDebugInfo() == this.GetDebugInfo();
    }

    public override int GetHashCode()
    {
        return GetDebugInfo().GetHashCode();
    }

}

public enum GameStatus
{
    Playable, Completed, Checkmate, DeadEnd
}

