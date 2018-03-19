using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player9 : Player
    {

        Boolean debugTalks = true;                  // Whether the AI's dialogue prints to the screen



        List<PlayerAction> prevPlayerActionList;    // Keeps track of the player action list independently
        int prevMoney;                              // Keeps track of the money the player had last action
        Card[] prevHand;                            // Keeps track of the player's hand from the previous round
        bool folded;                                // A flag to note whether the player folded


        // Profile on opposing 
        String oppName;                             // The name of the opponent
        List<int> Bets1;                            // How much the opponent bets each first phase
        List<int> Bets2;                            // How much the opponent bets each second phase
        List<int> winBets;                          // How much the opponent bets in rounds they win
        List<int> loseBets;                         // How much the opponent bets in rounds they lose
        List<int>[] handBets;                       // Bets corresponding to opponent hand values
        List<int> p1Folds;                          // Amounts of money bet in the first round that cause the opponent to fold
        List<int> bets;                             // Amounts of money that the opponent bets
        List<int> raises;                             // Amounts of money that the opponent raises
        int roundCounter;                           // How many rounds we've gone

        Card highCard;  //the high card



















        /************************\
         *
         *  Constructing and
         *  Initializing
         * 
        \************************/


        public Player9(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
            InitVariables();
        }





        /// <summary>
        /// Initializes the values of the opponent's profile
        /// Used at the very beginning,
        /// and then whenever a new opponent joins the game
        /// </summary>
        void InitVariables()
        {
            folded = false;

            oppName = "there is no name here please go away";
            Bets1 = new List<int>();
            Bets2 = new List<int>();
            winBets = new List<int>();
            loseBets = new List<int>();
            handBets = new List<int>[10];
            for (int n = 0; n < handBets.Length; ++n)
                handBets[n] = new List<int>();
            p1Folds = new List<int>();
            roundCounter = 0;
            bets = new List<int>();
            raises = new List<int>();
        }



























        /************\
         * 
         * Betting
         * 
        \************/



        /*
         * 
         * Random notes on betting:
         * 
         * - Start out by keeping bets low and avoiding folding when possible
         *   Allowing rounds to play out to their ends allows the most data collection
         *   
         *
         * 
         */




        /*
        * The built in Player methods
        * Redirecting Betting to Bet Method
        */
        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            return Bet(actions, hand, "Bet1");
        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            return Bet(actions, hand, "Bet2");
        }

        public override PlayerAction Draw(Card[] hand)
        {

            int handValue = Evaluate.RateAHand(hand, out highCard);                 // Gets the value of the hand we have
            Evaluate.SortHand(hand);                                                // Sorts hand

            if (handValue == 10 || handValue == 9 || handValue == 7 || handValue == 6 || handValue == 5)
            {
                Speak("I swap nothing, mortal");
                return new PlayerAction(Name, "Draw", "stand pat", 0);
            }
            if (handValue == 8)
            {
                Speak("I swap one card");

                //find card that isn't part of 4 of a kind and remove it
                for (int i = 2; i < 15; i++)
                {
                    if (Evaluate.ValueCount(i, hand) == 4)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            if (hand[j].Value != i)
                            {
                                hand[j] = null;
                            }
                        }
                    }
                }

                return new PlayerAction(Name, "Draw", "draw", 1);
            }
            if (handValue == 4)
            {
                Speak("I swap two cards");
                List<int> cardsToRemove = new List<int>();

                //find cards that aren't part of three of a kind and remove them
                for (int i = 2; i < 15; i++)
                {
                    if (Evaluate.ValueCount(i, hand) == 3)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            if (hand[j].Value != i)
                            {
                                cardsToRemove.Add(j);
                                //hand[j] = null;
                            }
                        }
                    }
                }

                //remove the ones we need to
                hand[cardsToRemove[0]] = null;
                hand[cardsToRemove[1]] = null;

                //request 2 cards
                return new PlayerAction(Name, "Draw", "draw", 2);
            }
            if (handValue == 3)
            {
                Speak("I swap one card");

                //find card that isn't part of either pair and remove it
                // Get the first pair
                int firstPair = 0;
                for (int i = 2; i < 15; i++)
                {
                    if (Evaluate.ValueCount(i, hand) == 2)
                    {
                        firstPair = i;
                    }
                }

                // now get the second pair
                int secondPair = 0;
                for (int i = 2; i < 15; i++)
                {
                    if (i == firstPair) continue; // skip this value
                    if (Evaluate.ValueCount(i, hand) == 2)
                    {
                        secondPair = i;
                    }
                }
                for (int i = 0; i < 5; i++)
                {
                    if (hand[i].Value != firstPair && hand[i].Value != secondPair)
                    {
                        hand[i] = null;
                    }
                }

                return new PlayerAction(Name, "Draw", "draw", 1);
            }
            if (handValue == 2)
            {
                //find the value that is the pair
                int pair = 0;
                for (int i = 2; i < 15; i++)
                {
                    if (Evaluate.ValueCount(i, hand) == 2)
                    {
                        pair = i;
                    }
                }

                //find the cards that are not the pair and replace them
                for (int i = 0; i < 5; i++)
                {
                    if (hand[i].Value != pair)
                    {
                        hand[i] = null;
                    }
                }

                Speak("I swap three cards");
                return new PlayerAction(Name, "Draw", "draw", 3);
            }
            if (handValue == 1)
            {
                Speak("I swap four cards");

                //Checks for 3 or more of same suit
                string flushSuit = "";
                int suitCount = 0;
                for (int i = 0; i < hand.Length; i++)
                {
                    for (int j = 0; j < hand.Length; j++)
                    {
                        if (hand[i].Suit == hand[j].Suit && i != j)
                        {
                            suitCount++;
                        }
                    }
                    if (suitCount >= 3)
                    {
                        flushSuit = hand[i].Suit;
                        break;
                    }
                }
                if (flushSuit != "")
                {
                    int count = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        if (hand[i].Suit != flushSuit)
                        {
                            hand[i] = null;
                            count++;
                        }
                    }
                    Console.WriteLine("requesting " + (5 - count) + " new cards");
                    return new PlayerAction(Name, "Draw", "draw", count);
                }

                // checks for a hand that is almost a straight
                // ex: 4, 5, 7, 8, Ace| 4, 5, 6, 7, 10| 
                // so Two in a row, then 1 missing, then Two more in a row
                // or Three in a row, 1 missing, then 1 
                // or Four in a row 
                if (hand[0].Value == hand[1].Value - 1 &&
                    hand[0].Value == hand[2].Value - 2 &&
                    hand[0].Value == hand[3].Value - 3 &&
                    hand[0].Value != hand[4].Value - 4)
                {
                    hand[4] = null;
                    return new PlayerAction(Name, "Draw", "draw", 1);
                }
                else if (hand[0].Value == hand[1].Value - 1 &&
                    hand[0].Value == hand[2].Value - 2 &&
                    hand[0].Value != hand[3].Value - 3 &&
                    hand[0].Value == hand[4].Value - 4)
                {
                    hand[3] = null;
                    return new PlayerAction(Name, "Draw", "draw", 1);
                }
                else if (hand[0].Value == hand[1].Value - 1 &&
                    hand[0].Value != hand[2].Value - 2 &&
                    hand[0].Value == hand[3].Value - 3 &&
                    hand[0].Value == hand[4].Value - 4)
                {
                    hand[2] = null;
                    return new PlayerAction(Name, "Draw", "draw", 1);
                }
                else if (hand[0].Value != hand[1].Value - 1 &&
                    hand[0].Value == hand[2].Value - 2 &&
                    hand[0].Value == hand[3].Value - 3 &&
                    hand[0].Value == hand[4].Value - 4)
                {
                    hand[1] = null;
                    return new PlayerAction(Name, "Draw", "draw", 1);
                }
                else if (hand[1].Value != hand[2].Value - 1 &&
                    hand[1].Value == hand[3].Value - 2 &&
                    hand[1].Value == hand[4].Value - 3 &&
                    hand[1].Value == hand[0].Value + 1)
                {
                    hand[0] = null;
                    return new PlayerAction(Name, "Draw", "draw", 1);
                }


                else
                {
                    return new PlayerAction(Name, "Draw", "draw", 4);
                }
            }
            else
            {
                return new PlayerAction(Name, "Draw", "stand pat", 0);
            }
        }






        /// <summary>
        /// The basic BET method, which can probably be used for betting in both rounds. Probably.
        /// </summary>
        /// <param name="actions">The current list of PlayerActions</param>
        /// <param name="hand">This player's current hand</param>
        /// <param name="phase">Which phase of betting this is - "Bet1" or "Bet2"</param>
        /// <returns></returns>
        PlayerAction Bet(List<PlayerAction> actions, Card[] hand, String phase)
{

    // Gathers data for opponent profiling
    // at the end of every round.
    // Then updates data values
    if (prevPlayerActionList == null || prevPlayerActionList.Count > actions.Count)
    {
        checkIfNewOpponent(actions);
        if (prevPlayerActionList != null)
            NewRound();                                             // Notes when a new round happens
    }
    prevPlayerActionList = actions;
    prevMoney = Money;
    prevHand = hand;
    folded = false;




    // This returns an analysis struct with info on the opponent
    // See Analysis struct declaration for more info on properties
    Analysis analysis = AnalyzeOpponent();
    Speak("Reliability: " + analysis.reliability + "\n\t~~~   Confidence: " + analysis.confidence + "\n\t~~~   Hand Guess: " + analysis.handGuess + "\n\t~~~   Bet To Fold: " + analysis.betToFold);






    // FOLDs when phase isn't a valid value
    // (Note that I set the folded boolean to true - I need this for opponent profiling)
    // (Also note that for each thing I add the action to the prevPlayerActionList
    //  before returning it - this is for use in my opponent profiling-
    //  it gives me more data to work with)
    if (phase != "Bet1" && phase != "Bet2")
    {
        Speak("I was given an invalid phase, and will fold");
        PlayerAction act = new PlayerAction(Name, "Bet1", "fold", 0);
        prevPlayerActionList.Add(act);
        folded = true;
        return act;
    }

    // BETs if no actions have been taken yet in this round and rank is greater than 1
    if (actions.Count == 0)
    {
    	PlayerAction act;
    	if(hand.Rank > 1)
    	{
        	Speak("I bet 15, human");
        	act = new PlayerAction(Name, phase, "bet", 15);
    	} else {
    		Speak("I check, human");
        	act = new PlayerAction(Name, phase, "check", 0);
    	}
        prevPlayerActionList.Add(act);
        return act;
    }




    // Gets a value that roughly represents the guessed advantage of the opponent
    float oppAdvantage = analysis.handGuess - hand.Rank;
    if(oppAdvantage < 0) oppAdvantage = 0;
    if(oppAdvantage > 2) oppAdvantage = 2;

    // Gets a value for the strength we have from 2 separate areas:
    // The analysis, who's weight increases each round, mulitplies the confidence by 10 and the above opp advantage to get a value between 0 and 1
    float analysisStrength = 1.0f - ((float)analysis.confidence * 0.1f * oppAdvantage); // A reversal of the strength of the opponent
    // Your hand, each rank of which adds 25% to the player's "strength"
    float handStrength = (hand.Rank-1) * 0.25f;	// 25% with a pair, 50% with two pair
    if(handStrength > 1) handStrength = 1;
    // Adds the two values to hopefully get a number between 0 and 1
    float strength = analysis.reliability * analysisStrength + (1-analysis.reliability) * handStrength;



            // BETs if the previous action was not a bet
            // only if our speculated opponent hand rank is lower or equal
            PlayerAction prev = actions[actions.Count - 1];
            if (prev.ActionName != "bet" &&
                prev.ActionName != "raise" &&
                prev.ActionName != "call" &&
                analysis.handGuess <= hand.Rank)
            {


                if(strength <= 25)
                {
                    Speak("I check, human");
                    PlayerAction check = new PlayerAction(Name, phase, "check", 0);
                    prevPlayerActionList.Add(check);
                    return check;
                }




                //decide how much to bet, based on analysis
                //reliability determines how much this affects our stock values (10 normally, 20 on a stretch)
                //Confidence is 50%
                //Hand estimate is 50%
                //These will combine to form the bet "offset" which is multiplied by reliability
                float confidenceFactor = analysis.confidence - 3 / 2.0f;
                float handGuessFactor = Math.Abs(analysis.handGuess - hand.Rank) / 10.0f;

                int betOffset = (int)((handGuessFactor + confidenceFactor) * (analysis.reliability * 30));

                int betAmount = 15 + betOffset;
                Speak("I bet " + betAmount + ", human");
                PlayerAction act = new PlayerAction(Name, phase, "bet", betAmount);
                prevPlayerActionList.Add(act);
                return act;
            }

            // If the previous action was a bet




            int cBet = FindActionBet(prevPlayerActionList.Count - 1);

    if(strength >= 75)
    {
    	Speak("I raise, human");
    	PlayerAction act = new PlayerAction(Name, phase, "raise", 30);
    	prevPlayerActionList.Add(act);
    	return act;
    }
    if((strength >= 50 && cBet <= 30) || cBet <= 15)
    {
    	Speak("I call, human");
    	PlayerAction act = new PlayerAction(Name, phase, "call", 0);
    	prevPlayerActionList.Add(act);
    	return act;
    }

    Speak("I fold, human");
    folded = true;
    PlayerAction a = new PlayerAction(Name, phase, "fold", 0);
    prevPlayerActionList.Add(a);
    return a;

    // CALLs if the previous action was a bet
    
}































/****************************\
 * 
 *  Opponent Profiling
 *  
 ****************************/







/// <summary>
/// When a new round happens, looks over bets of previous round and gathers data
/// </summary>
void NewRound()
{
    ++roundCounter;

    // Goes through each of the opponents bets
    // Adds them to bet totals for each round
    // Notes each bet and raise
    int roundBet1 = 0;
    int roundBet2 = 0;
    for (int n = 0; n < prevPlayerActionList.Count; ++n)
    {
        PlayerAction a = prevPlayerActionList[n];
        if (a.Name != Name)
        {
            if (a.ActionPhase == "Bet1")
                roundBet1 += FindActionBet(n);
            else if (a.ActionPhase == "Bet2")
                roundBet2 += FindActionBet(n);
            if (a.ActionName == "bet")
                bets.Add(a.Amount);
            else if (a.ActionName == "raise")
                raises.Add(a.Amount);
        }
    }
    Bets1.Add(roundBet1);
    Bets2.Add(roundBet2);




    Card highCard;
    int handValue = Evaluate.RateAHand(prevHand, out highCard);



    // If the AI wins and the opponent loses
    // If the opponent folded in the first phase,
    // we store how much the bet was that made them fold
    // The total bet is added to the list of bets during lost rounds
    if (prevMoney < Money)
    {
        PlayerAction lastAct = prevPlayerActionList[prevPlayerActionList.Count - 1];
        if (lastAct.ActionPhase == "Bet1")
            p1Folds.Add(FindActionBet(prevPlayerActionList.Count - 1));
        loseBets.Add(roundBet1 + roundBet2);
    }
    // If the AI loses and the opponent wins
    // If the AI didn't fold, that means the opponent had a better hand
    // We can determine that the opponent's hand was at least the same value as the AI's
    // So they're total bet on it is added to all possible hand values
    else
    {
        winBets.Add(roundBet1 + roundBet2);
        if (folded == false)
        {
            Speak("Your hand was at least a value of " + handValue + " and you bet " + (roundBet1 + roundBet2));
            for (int n = handValue - 1; n < 10; ++n)
                handBets[n].Add(roundBet1 + roundBet2);
        }
    }
}




/// <summary>
/// Finds the bet of the PlayerAction at the given index of the prevPlayerActions list
/// Determines bet amount based on whether it's a bet, call, or raise
/// </summary>
/// <param name="index">Index of Player Actions</param>
/// <returns>Bet amount</returns>
int FindActionBet(int index)
{
    PlayerAction a = prevPlayerActionList[index];
    int bet = 0;
    // If the PlayerAction is a bet, that's this action's amount
    if (a.ActionName == "bet")
        bet = a.Amount;
    // If it's a call, the previous Action is this action's amount
    //      (with special circumstances for raising)
    else if (a.ActionName == "call")
    {
        bet = prevPlayerActionList[index - 1].Amount;
        if (prevPlayerActionList[index - 1].ActionName == "raise")
            bet += prevPlayerActionList[index - 2].Amount;
    }
    // If it's a raise, the raise and previous bet are this action's amount
    else if (a.ActionName == "raise")
        bet = a.Amount + prevPlayerActionList[index - 1].Amount;

    return bet;
}





/// <summary>
/// Checks if the opponent in the input actions list is a new opponent
/// By comparing the opponent name
/// If found to be different, resets the opponent profile
/// </summary>
/// <param name="actions">Current list of PlayerActions</param>
void checkIfNewOpponent(List<PlayerAction> actions)
{
    if (prevPlayerActionList == null)
    {
        InitVariables();
        oppName = "challenger";
        Speak("A new challenger approaches!");
        return;
    }

    for (int n = 0; n < actions.Count; ++n)
    {
        PlayerAction a = actions[n];
        if (a.Name != Name && a.Name != oppName)
        {
            InitVariables();
            oppName = a.Name;
            Speak("A new challenger approaches!");
            return;
        }
    }
}





/// <summary>
/// An analysis object that AnalyzeOpponent() will return
/// </summary>
struct Analysis
{
    /// <summary>
    /// How reliable this analysis is,
    /// based on how many rounds have been played
    /// Ranges from 0 to 1
    /// </summary>
    public float reliability;

    /// <summary>
    /// How much can we bet to get the opponent to fold
    /// -1 is returned if there is not enough data
    /// This thing as a whole is probably too simple to be useful, honestly
    /// </summary>
    public int betToFold;

    /// <summary>
    /// What hand we think the opponent has
    /// Range of 1 to 10
    /// Returns -1 if not able to guess
    /// </summary>
    public int handGuess;

    /// <summary>
    /// How confident the opponent is:
    /// 1 - Not confident
    /// 3 - Neutral
    /// 5 - Confident
    /// More than 5 - Very Confident
    /// </summary>
    public int confidence;
}


/// <summary>
/// Makes a guess at what hand the opponent has, based on their betting
/// </summary>
/// <returns>An analysis object with info about opponent</returns>
Analysis AnalyzeOpponent()
{
    Analysis analysis = new Analysis();
    analysis.reliability = (float)roundCounter / 60.0f;
    if (analysis.reliability > 1) analysis.reliability = 1;

    // Get data values
    float avgBet1 = AvgIntList(Bets1);
    float avgBet2 = AvgIntList(Bets2);

    float avgWinBet = AvgIntList(winBets);
    float avgLoseBet = AvgIntList(loseBets);

    float avgFoldBet = AvgIntList(p1Folds);
    float maxFoldBet = (p1Folds.Count > 0) ? p1Folds.Max() : -1;
    analysis.betToFold = (int)Math.Round((avgFoldBet + maxFoldBet) / 2);

    int betMode = ModeIntList(bets);
    int raiseMode = ModeIntList(raises);






    // Start by finding out how much they have bet so far
    int roundBet1 = 0;
    int roundBet2 = 0;
    List<int> roundBets = new List<int>();
    List<int> roundRaises = new List<int>();
    int betsAboveUsual = 0;
    int raisesAboveUsual = 0;
    Boolean round2 = false;
    for (int n = 0; n < prevPlayerActionList.Count; ++n)
    {
        PlayerAction a = prevPlayerActionList[n];
        if (a.Name != Name)
        {
            if (a.ActionPhase == "Bet1")
                roundBet1 += FindActionBet(n);
            else if (a.ActionPhase == "Bet2")
            {
                roundBet2 += FindActionBet(n);
                round2 = true;
            }
            if (a.ActionName == "bet")
            {
                roundBets.Add(a.Amount);
                betsAboveUsual += (a.Amount > betMode) ? 1 : 0;
            }
            else if (a.ActionName == "raise")
            {
                roundRaises.Add(a.Amount);
                raisesAboveUsual += (a.Amount > raiseMode) ? 1 : 0;
            }
        }
    }
    int totalBet = (round2 == true) ? roundBet1 + roundBet2 : roundBet1;




    // Next, finds out if betting more than usual
    Boolean bettingMore;
    bettingMore = (totalBet > avgBet1 + avgBet2);
    // Determines whether they bet more when winning and less when losing
    // (whether they bet just based on hand value or try to bluff and string their opponent along)
    Boolean betsMoreWhenGood = (avgBet1 + avgBet2 < avgWinBet);
    Boolean betsLessWhenBad = (avgBet1 + avgBet2 > avgLoseBet);
    // Determines if the opponent is betting confidently
    int confidence = 3;
    if (betsMoreWhenGood == bettingMore) confidence += 1;
    else confidence -= 1;
    if (betsLessWhenBad == bettingMore) confidence += 1;
    else confidence -= 1;
    confidence += betsAboveUsual;
    confidence += raisesAboveUsual;
    analysis.confidence = confidence;




    // Next guesses what hand they have based on previous betting
    int closestAvgIndex = 0;
    float closestAvgDist = float.MaxValue;
    for (int n = 0; n < handBets.Length; ++n)
    {
        List<int> bets = handBets[n];

        if (bets.Count > 0)
        {
            float betAvg = AvgIntList(bets);
            if (betAvg > -1)
            {
                float diff = Math.Abs(totalBet - betAvg);
                if (diff < closestAvgDist)
                {
                    closestAvgIndex = n;
                    closestAvgDist = diff;
                }
            }
        }
    }
    analysis.handGuess = closestAvgIndex + 1;

    return analysis;
}




















/********************\
 * 
 * Helpers
 * 
\********************/




/// <summary>
/// Delivers cool formatted dialogue from the AI, 
/// for the purposes of understanding what it is doing.
/// Will only print dialogue if debugTalks = true
/// </summary>
/// <param name="input">The dialogue for the AI to say</param>
void Speak(String input)
{
    if (debugTalks == false) return;

    int length = input.Length;
    String output = "\n\t";
    for (int n = 0; n < length + 12; ++n)
        output += "~";
    output += "\n\t~~~  \"" + input + "\"  ~~~\n\t";
    for (int n = 0; n < length + 12; ++n)
        output += "~";
    output += "\n";
    Console.WriteLine(output);
}

/// <summary>
/// Gets the average of a list of ints
/// </summary>
/// <param name="list"></param>
/// <returns></returns>
float AvgIntList(List<int> list)
{
    if (list == null) return -1;
    if (list.Count == 0) return 0;

    float avg = 0;
    for (int n = 0; n < list.Count; ++n)
        avg += (float)list[n];
    avg /= (float)list.Count;
    return avg;
}

/// <summary>
///  Gets the mode of a list of ints
/// </summary>
/// <param name="list"></param>
/// <returns></returns>
int ModeIntList(List<int> list)
{
    if (list.Count == 0) return -1;

    Dictionary<int, int> dict = new Dictionary<int, int>();

    int count = -1;
    int num = -1;

    for (int n = 0; n < list.Count; ++n)
    {
        if (!dict.ContainsKey(list[n]))
            dict.Add(list[n], 0);
        dict[list[n]] += 1;
        if (dict[list[n]] > count)
        {
            count = dict[list[n]];
            num = list[n];
        }
    }

    return num;
}


/// <summary>
/// Generates a random list of ints
/// Displays it
/// For debugging purposes
/// </summary>
/// <param name="length"></param>
/// <returns></returns>
List<int> RandomIntList(int length)
{
    String output = "{ ";
    List<int> list = new List<int>();
    Random rand = new Random();
    for (int n = 0; n < length; ++n)
    {
        int random = rand.Next(0, 20);
        list.Add(random);
        output += random + ", ";
    }
    output += "}";
    Console.WriteLine(output);
    return list;
}

    }
}


