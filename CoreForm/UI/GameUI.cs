﻿using FreeCellSolitaire.Core.CardModels;
using FreeCellSolitaire.Core.GameModels;
using FreeCellSolitaire.Data;
using FreeCellSolitaire.Entities.GameEntities;
using FreeCellSolitaire.Properties;
using FreeCellSolitaire.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.Json;
using System.Windows.Forms;

namespace CoreForm.UI
{
    /// <summary>
    /// UI抽象層，邏輯,事件與互動
    /// </summary>
    public interface IGameUI
    {
        void InitScreen(int ratio);
        void ResizeScreen(int width, int height);
        
        bool Move(string notation);
        
        void StartGame();
        void PickNumberStartGame();
        void RestartGame(bool confirm);        
        void QuitGame();
        bool QuitGameConfirm();
        void AbouteGame();
        void StatisticalResult();
        int CreateScripts(out Queue<string> scripts);
        void BackToPreviousStep();
        void SelectOrMove(string code);
        int? GetUnfinshedCardCount();        
        Image GetCardImage(Card card);

        void LogDebug(string message);
        GeneralColumnPanel GetSelectedColumn();
        Column GetColumn(Type type, int index);
        IGame GetGame();
        public int? GameNumber { get; set; }
        int SteppingNumber { get; }
        void SetStartedCallback(Action act);
        void SetMovedCallback(Action act);
        bool IsPlaying();
    }


    public class GameUI : IGameUI
    {
        IGameForm _form;
        DialogManager _dialog;
        IGame _game;
        DateTime _startTime;
        Stack<IGame> _archives = new Stack<IGame>();
        private TableauContainer _tableauUI;
        private HomecellsContainer _homecellsUI;
        private FoundationsContainer _foundationsUI;

        int _defaultWidth = 800;
        int _defaultHeight = 600;
        int _ratio;

        Action _movedCallback;
        Action _startedCallback;
        List<string> _debugLog = new List<string>();

        public GameUI(IGameForm form, DialogManager dialog)
        {
            this._form = form;
            this._dialog = dialog;
            this._game = null;;

            InitImages();
        }

        /// <summary>
        /// 初始空畫面
        /// </summary>
        /// <param name="menuHeight"></param>
        public void InitScreen(int ratio)
        {
            int width = _defaultWidth * ratio / 100;
            int height = _defaultHeight * ratio / 100;
            _ratio = ratio;
            //空畫面            
            InitBoardScreen(width, height);

            InitControls();
        }

        public void ResizeScreen(int width, int height)
        {
            _cardWidth = (int)(Math.Floor((decimal)_form.ClientSize.Width / 9));
            _cardHeight = (int)(_cardWidth * 1.38);

            //Resize Controls
            Rectangle rect = new Rectangle(0, _layoutMarginTop, _form.ClientSize.Width, _form.ClientSize.Height);
            _foundationsUI.ResizeTo(rect, dock: 1, _cardWidth, _cardHeight);
            _homecellsUI.ResizeTo(rect, dock: 2, _cardWidth, _cardHeight);
            _tableauUI.ResizeTo(rect, dock: 3, _cardWidth, _cardHeight);
            this.Redraw();
        }

        private int _layoutMarginTop = 40;
        private int _cardWidth;
        private int _cardHeight;
        private void InitBoardScreen(int boardWidth, int boardHeight)
        {
            _form.Width = boardWidth;
            _form.Height = boardHeight;
            _form.BackColor = Color.FromArgb(0, 123, 0);
  
            _cardWidth = (int)(Math.Floor((decimal)_form.ClientSize.Width / 9));
            _cardHeight = (int)(_cardWidth * 1.38);
        }

        private void InitControls()
        {
            Rectangle rect = new Rectangle(0, _layoutMarginTop, _form.ClientSize.Width, _form.ClientSize.Height);

            this._foundationsUI = new FoundationsContainer(this,
                columnNumber: 4, rect, dock: 1, _cardWidth, _cardHeight);
            this._form.SetControlReady(this._foundationsUI);


            this._homecellsUI = new HomecellsContainer(this,
                columnNumber: 4, rect, dock: 2, _cardWidth, _cardHeight);
            this._form.SetControlReady(this._homecellsUI);


            this._tableauUI = new TableauContainer(this,
                columnNumber: 8, rect, dock: 3, _cardWidth, _cardHeight);
            this._form.SetControlReady(this._tableauUI);
        }



        private string _selectedNotation;
        private DateTime lastClickedTime;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="forceMove"></param>
        public void SelectOrMove(string code)
        {            
            //_form.SetCaption(_selectedNotation + "/" + code + "/" + DateTime.Now.Ticks);
            //遊戲還未初始不作用
            if (this._game == null || this._game.IsPlaying() == false)
            {
                return;
            }
            if (_selectedNotation == "" && code[0] == 'h')
            {
                Select("");
                return;
            }

            bool tryMoveToFoundations = false;

            if (code == _selectedNotation)
            {                
                tryMoveToFoundations = (DateTime.Now - lastClickedTime).TotalSeconds <= 0.7d && code[0] == 't';
                if (tryMoveToFoundations)
                {
                    for (int i = 0; i < _game.Foundations.ColumnCount; i++)
                    {
                        if (_game.Foundations.GetColumn(i).GetCardsCount() == 0)
                        {
                            _selectedNotation += _game.Foundations.GetColumn(i).Code;
                            if (Move(_selectedNotation))
                            {
                                _selectedNotation = "";
                                break;
                            }
                        }
                    }
                }
                else
                {
                    _selectedNotation = "";
                    Select("");             
                }
            }
            else
            {
                _selectedNotation += code;
                if (_selectedNotation.Length == 2)
                {
                    Select(_selectedNotation);
                }
                else if (Move(_selectedNotation))
                {
                    this._selectedNotation = "";
                }
                else
                {
                    this._selectedNotation = "";
                    Select("");
                }
            }
            lastClickedTime = DateTime.Now;
        }
        public void Deselect(string columnCode)
        {
            this._selectedNotation = "";
            Redraw("");
        }
        public void Select(string columnCode)
        {
            _selectedColumn = null;

            List<GeneralColumnPanel> columnPanels = new List<GeneralColumnPanel>();
            columnPanels.AddRange(_tableauUI.GetColumnPanels());
            columnPanels.AddRange(_foundationsUI.GetColumnPanels());            
            foreach (var columnPanel in columnPanels)
            {
                if (columnPanel.Code == columnCode && columnPanel.GetCardControlCount() > 0)
                {
                    _selectedColumn = columnPanel;
                    break;
                }
            }
            //TODO 之後會調整傳入參數型別， 改為GeneralColumnPanel 
            Redraw(columnCode);
        }

        public bool Move(string notation)
        {
            bool moved = false;
            var clone = _game.Clone();
            _game.Move(notation);
            moved = _game.Equals(clone) == false;
            if (moved)
            {
                _archives.Push(clone);
                if (_movedCallback != null)
                {
                    _movedCallback();
                }
                Redraw();
                _selectedColumn = null;
            }
            ControlFlow();
            return moved;
        }
        /// <summary>
        /// 開始、重新開始，選擇特定關卡開始
        /// </summary>
        /// <param name="deckNo"></param>
        private void Start(int? deckNo)
        {
            Stop();

            _game = new Game { EnableAssist = true };
            var tableau = new Tableau(_game);
            var homecells = new Homecells(_game);
            var foundations = new Foundations(_game);
          
            var deck = _game.Deck.Shuffle(deckNo);
            tableau.Init(deck);
            this.GameNumber = deck.Number;

            _tableauUI.Clear();
            _homecellsUI.Clear();
            _foundationsUI.Clear();

            this.Redraw();

            this.Reset();

            if (_startedCallback != null)
            {
                Console.WriteLine($"start game: {this._game.Deck.Number}");
                _startedCallback();
            }
            this._startTime = DateTime.Now;
            this.lastClickedTime = default(DateTime);
        }
        private void Stop()
        {
            if (_game != null && _game.IsPlaying() && this.IsPlaying())
            {
                this.SaveRecord();
                this._game = null;
            }
        }

        private void SaveRecord()
        {
            string recordFilePath = Helper.MapPath("record.csv");
            var gameUser = new PersonalRecord(recordFilePath);
            bool success = this._game.EstimateGameover(false) == GameStatus.Completed;
            string track = string.Join(',', this._game.Tracks.Select(x => x.Notation).ToArray());
            double elapsedSecs = Math.Round((DateTime.Now - this._startTime).TotalSeconds, 2);
            GameRecord record = gameUser.AddRecord(_game.Deck.Number,_startTime, elapsedSecs, this._game.Tracks.Count, 
                success, track, "");
            gameUser.Save();

            string json = System.Text.Json.JsonSerializer.Serialize(record, new JsonSerializerOptions
                { WriteIndented = true });
            Console.WriteLine(json);
        }

        private void Reset()
        {
            this._startTime = DateTime.Now;
            this._selectedNotation = "";
        }

        private void ControlFlow()
        {
            GameStatus status = _game.EstimateGameover(false);
            if (status == GameStatus.Completed)
            {
                Stop();
                var choice = _dialog.ShowYouWinContinueDialog(210 * _ratio / 100);
                if (choice.Reuslt == DialogResult.Yes)
                {
                    if (choice.CheckedYes)
                    {
                        PickNumberStartGame();
                    }
                    else
                    {
                        StartGame();
                    }
                }
            } 
            else if (status == GameStatus.Checkmate)
            {
                _form.AlertCheckMate();
            }
            else if (status == GameStatus.DeadEnd)
            {
                var choice = _dialog.ShowGameoverContinueDialog(210 * _ratio/100);
                //相同排局
                if (choice.CheckedYes)
                {
                    this.RestartGame(false);                    
                }
                else
                {
                    PickNumberStartGame();
                }
            }
        }

        public int? GameNumber { get; set; }

        public int SteppingNumber
        {
            get
            {
                return _archives.Count;
            }
        }

        private void Redraw(string columnCode = null)
        {
            //如果遊戲還沒開始，_game資料還未初始化，此判斷避免此時重繪會有錯誤
            if (_game.Tableau == null)
            {
                return;
            }
            string log = "";
            DateTime now0 = DateTime.Now;
            DateTime now = DateTime.Now;            
            RedrawTableau();
            log += "Redraw T:" + (DateTime.Now - now).TotalSeconds.ToString("0.00");now = DateTime.Now;
            RedrawHomecells();
            log += ", H:" + (DateTime.Now - now).TotalSeconds.ToString("0.00"); now = DateTime.Now;
            now = DateTime.Now;
            RedrawFoundations();
            log += ", F:" + (DateTime.Now - now).TotalSeconds.ToString("0.00"); now = DateTime.Now;
            now = DateTime.Now;
            RedrawActivedCard(columnCode);            
            log += ", A:" + (DateTime.Now - now).TotalSeconds.ToString("0.00"); now = DateTime.Now;
            log += ", Total:" + (DateTime.Now - now0).TotalSeconds.ToString("0.0000"); now = DateTime.Now;
            LogDebug(log);
        }

        private void RedrawActivedCard(string activeColumnCode)
        {
            _foundationsUI.SetActiveColumn(activeColumnCode);
            _tableauUI.SetActiveColumn(activeColumnCode);
        }

        private void RedrawTableau()
        {
            var _tableau = _game.Tableau;
            for (int columnIndex = 0; columnIndex < _tableau.ColumnCount; columnIndex++)
            {
                List<CardView> cards = new List<CardView>();
                for (int cardIndex = 0; cardIndex < _tableau.GetColumn(columnIndex).GetCardsCount(); cardIndex++)
                {
                    cards.Add(_tableau.GetColumn(columnIndex).GetCard(cardIndex));
                }
                _tableauUI.RedrawCards(columnIndex, cards);
            }
        }

        private void RedrawFoundations()
        {
            var _foundations = _game.Foundations;
            for (int columnIndex = 0; columnIndex < _foundations.ColumnCount; columnIndex++)
            {
                List<CardView> cards = new List<CardView>();
                for (int cardIndex = 0; cardIndex < _foundations.GetColumn(columnIndex).GetCardsCount(); cardIndex++)
                {
                    cards.Add(_foundations.GetColumn(columnIndex).GetCard(cardIndex));
                }
                _foundationsUI.RedrawCards(columnIndex, cards);
            }
        }

        private void RedrawHomecells()
        {
            var _homecells = _game.Homecells;
            for (int columnIndex = 0; columnIndex < _homecells.ColumnCount; columnIndex++)
            {
                List<CardView> cards = new List<CardView>();
                for (int cardIndex = 0; cardIndex < _homecells.GetColumn(columnIndex).GetCardsCount(); cardIndex++)
                {
                    cards.Add(_homecells.GetColumn(columnIndex).GetCard(cardIndex));
                }
                _homecellsUI.RedrawCards(columnIndex, cards);
            }
        }

        public void StartGame()
        {
            if (_game != null && _game.IsPlaying() &&
                MessageBox.Show("是否放棄這一局", "新接龍", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
            Start(null);            
        }

        public void PickNumberStartGame()
        {            
            var gameNumber = Deck.GetRandom();
            var dialogResult = _dialog.ShowSelectGameNumberDialog(210 * _ratio / 100, gameNumber);
            if (dialogResult.Reuslt == DialogResult.Yes)
            {
                Start(int.Parse(dialogResult.ReturnText));
                Redraw();
            }
        }

        public void RestartGame(bool confirm)
        {
            if (confirm && MessageBox.Show("是否放棄這一局", "新接龍", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }
            Start(GameNumber);
        }


        public bool QuitGameConfirm()
        {
            if (_game != null && _game.IsPlaying() &&
                MessageBox.Show("是否放棄這一局", "新接龍", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return false;
            }
            Stop();
            return true;
        }


        public void QuitGame()
        {
            // 會呼叫到 QuitGameConfirm，裏面會執行Stop，這邊就不要需要
            _form.Close();
        }

        public void AbouteGame()
        {
            _dialog.ShowAboutGameDialog();
        }

        public void StatisticalResult()
        {
            string recordFilePath = Helper.MapPath("record.csv");
            var summary = new PersonalRecord(recordFilePath).GetSummary(this.GameNumber.GetValueOrDefault());
            _dialog.ShowStatisticalResultDialog(summary
                , delegate()
                {
                    new PersonalRecord(recordFilePath).DeleteAll();
                });
        }

        public int CreateScripts(out Queue<string> scripts)
        {
            scripts = new Queue<string>();
            scripts.Enqueue("t1h0");
            scripts.Enqueue("t2t0");
            scripts.Enqueue("t7t3");
            scripts.Enqueue("t7t1");
            scripts.Enqueue("t7f0");
            scripts.Enqueue("t7t2");
            scripts.Enqueue("t7f1");
            scripts.Enqueue("t6f2");
            scripts.Enqueue("t2h3");
            scripts.Enqueue("t1h3");
            scripts.Enqueue("t2f0");
            scripts.Enqueue("f2t7");
            scripts.Enqueue("t2t7");
            scripts.Enqueue("t2t3");
            scripts.Enqueue("t2t5");
            scripts.Enqueue("t5f2");
            scripts.Enqueue("t5t7");
            scripts.Enqueue("f2t7");
            scripts.Enqueue("t0h2");
            scripts.Enqueue("t6f2");
            scripts.Enqueue("t5f3");
            scripts.Enqueue("t5t2");
            scripts.Enqueue("t5t2");
            scripts.Enqueue("t5h2");
            scripts.Enqueue("t4f0");
            scripts.Enqueue("t4t1");
            scripts.Enqueue("f2t5");
            scripts.Enqueue("t4f2");
            scripts.Enqueue("t4t5");
            scripts.Enqueue("t4t2");
            scripts.Enqueue("t6t2");
            scripts.Enqueue("f0t4");
            scripts.Enqueue("t0t4");
            scripts.Enqueue("t0t4");
            scripts.Enqueue("t0t6");
            scripts.Enqueue("f2h1");
            scripts.Enqueue("f3h2");
            scripts.Enqueue("f1t6");
            scripts.Enqueue("t3t4");
            scripts.Enqueue("t3h2");
            scripts.Enqueue("t3t2");
            scripts.Enqueue("t3f0");
            scripts.Enqueue("t3f1");
            scripts.Enqueue("t1f0");
            scripts.Enqueue("t1f1");
            return 1;            
        }

        public void SetMovedCallback(Action act)
        {
            _movedCallback = act;
        }

        public void BackToPreviousStep()
        {
            if (_archives.Count == 0)
            {
                return;
            }
            var archive = _archives.Pop();
            _game = archive;
            if (_movedCallback != null)
            {
                _movedCallback();
            }
            Redraw();
            ControlFlow();
        }

        public int? GetUnfinshedCardCount()
        {
            if (_game.Tableau == null)
            {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < _game.Tableau.ColumnCount; i++)
            {
                count += _game.Tableau.GetColumn(i).GetCardsCount();
            }
            for (int i = 0; i < _game.Foundations.ColumnCount; i++)
            {
                count += _game.Foundations.GetColumn(i).GetCardsCount();
            }
            return count;
        }

        public void SetStartedCallback(Action act)
        {
            _startedCallback = act;
        }

        public void LogDebug(string message)
        {
            _form.LogDebug(message);
        }

        Dictionary<Card, Image> _cardImages = new Dictionary<Card, Image>();
        private void InitImages()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {                
                for (int number = 1; number <= 13; number++)
                {
                    Card card = new Card(suit, number);
                    string cardResourceName = suit.ToString() + "-" + number.ToString("00");
                    Stream resource = assembly
                        .GetManifestResourceStream("FreeCellSolitaire.assets.img." + cardResourceName + ".png");
                    Image img = Image.FromStream(resource);
                    _cardImages.Add(card, img);
                }                
            }            
        }
        
        public Image GetCardImage(Card card)
        {
            Image img;
            _cardImages.TryGetValue(card, out img);
            return img;
        }

        private GeneralColumnPanel _selectedColumn = null;

        public GeneralColumnPanel GetSelectedColumn()
        {
            return _selectedColumn;
        }

        public Column GetColumn(Type type, int index)
        {
            if (this._game.IsPlaying() == false)
            {
                return null;
            }
            if (type == typeof(FoundationColumnPanel))
            {
                return _game.Foundations.GetColumn(index);
            }
            if (type == typeof(HomecellColumnPanel))
            {
                return _game.Homecells.GetColumn(index);
            }
            if (type == typeof(TableauColumnPanel))
            {
                return _game.Tableau.GetColumn(index);
            }
            return null;
        }

        public IGame GetGame()
        {
            return _game;
        }
        public bool IsPlaying()
        {
            return lastClickedTime != default(DateTime);
        }
    }

}
