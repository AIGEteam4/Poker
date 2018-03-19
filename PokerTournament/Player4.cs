using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player4 : Player
    {
        //Variables for the betting phases
        MoveSequence chooseMove = new MoveSequence();
        TurnDetails details = new TurnDetails();

        public Player4(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
            //initialize game details
            details = new TurnDetails(mny, Name);
        }

        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            //estimate values and update the rounds details
            Card highestCard;
            int rating = Evaluate.RateAHand(hand, out highestCard);
            details.Update(rating, "Bet1");

            //return the playeraction
            return chooseMove.calc(actions, details);
        }

        public override PlayerAction Draw(Card[] hand)
        {
            Card highCard;
            int rating = Evaluate.RateAHand(Hand, out highCard);

            int discard = new DiscardNode().Discard(ref hand, rating);

            return new PlayerAction(Name, "Draw", "draw", discard);
        }

        //Implemented by Mark Scott
        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            //estimate values and update the rounds details
            Card highestCard;
            int rating = Evaluate.RateAHand(hand, out highestCard);
            details.Update(rating, "Bet2");

            //return the playeraction
            return chooseMove.calc(actions, details);

            /* old Nelson Stuff
            //Rate the hand
            Card highestCard;
            int handRating = Evaluate.RateAHand(hand, out highestCard);

            //Selector
            PlayerAction pa = new PlayerAction(Name, "Bet2", "fold", 0);
            if (AllInSequence(handRating))
            {
                pa = new PlayerAction(Name, "Bet2", "raise", Money);
            }
            else if (RaiseSequence(handRating))
            {
                pa = new PlayerAction(Name, "Bet2", "raise", (int)(Money / 4));
            }
            else if (actions.Count > 0 && CallSequence(actions[actions.Count - 1], handRating))
            {
                pa = new PlayerAction(Name, "Bet2", "call", actions[actions.Count - 1].Amount);
            }
            else if (CheckSequence(handRating))
            {
                pa = new PlayerAction(Name, "Bet2", "check", 0);
            }

            return pa;
            */
        }
        
        //*************************************
        //Nelson's Behavior Tree Methods
        //*************************************

        #region Nelsons Behavior Tree (Not currently used)

        //Checks to see if we should go all in
        bool AllInSequence(int rating)
        {
            if (Money > 0 && rating >= 5)
            {
                return true;
            }
            return false;
        }

        //Should raise
        bool RaiseSequence(int rating)
        {
            if (Money >= (int)Money / 4 && rating > 1)
            {
                return true;
            }
            return false;
        }

        //Should call
        bool CallSequence(PlayerAction lastAction, int rating)
        {
            if (lastAction.ActionName == "raise" && rating > 0 && Money > lastAction.Amount)
            {
                return true;
            }
            return false;
        }

        //Just check
        bool CheckSequence(int rating)
        {
            if (rating > 0)
            {
                return true;
            }
            return false;
        }

        #endregion
    }

    //*************************************
    //Zane's Behavior Tree Nodes and Struct
    //*************************************

    #region Zane's Behavior Tree Content

    //Struct: Holds the values unique to this players turn for more advanced strategy determination
    struct TurnDetails
    {

        //Variables
        public int rating;
        public int money;
        public string name;
        public string phase;
        public int betAmount;

        //Constructor - only initializes money and the name
        public TurnDetails(int m, string n)
        {
            //based off of input
            money = m;
            name = n;

            //initialized after the hand is dealt
            rating = 0;
            phase = "";
            betAmount = 0;
        }

        //Update: This updates the phase and rating of the hand whenever a betting phase is called
        public void Update(int newRating, string newPhase)
        {
            phase = newPhase;
            rating = newRating;
        }
    }

    //Class: Base TurnNode. All members of the behavior tree inherit from this class
    class TurnNode
    {
        //calc: must be used by all children. All behavior logic is contained in these
        public virtual PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            return null;
        }

        //EstimateValueOfHand: This determines the monetary range the AI will work with in their betting
        public int estimateValueOfHand(TurnDetails details)
        {
            int max;

            //This is for the first betting phase
            if (details.phase == "Bet1")
            {
                max = 200;
                if (details.money < max) max = details.money;   //keeps the max value from exceeding the current money
                float percent1 = details.rating / 5f;
                max = (int)(max * percent1);

                return max;
            }

            //this raises the mentary range for the second betting phase
            max = 400;
            if (details.money < max) max = details.money;   //keeps the max value from exceeding the current money
            float percent2 = details.rating / 5f;
            max = (int)(max * percent2);

            return max;
        }
    }

    //Class: MoveSequence is used to run through the logic of all other nodes
    class MoveSequence: TurnNode
    {
        public MoveSequence()
        {
        }

        //The calc implementation in this class is the head of the behavior tree and returns the final PlayerAction determined
        public override PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            //The two different directions the tree can branch
            Bet bet = new Bet();
            Raise raise = new Raise();

            //only allows bet if it hasn't been called already, otherwise raise is called
            int actionIndex = actions.Count;
            if (actionIndex > 0)
                if (actions[actionIndex - 1].ActionName != "check")
                    return raise.calc(actions, details);

            return bet.calc(actions, details);
        }
    }

    //Check behavior
    class Check : TurnNode
    {
        //This calc implementation only allows the player do fold instead of check in the second round.
        public override PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            //Bet1 always returns a check from here
            if(details.phase == "Bet1")
                return new PlayerAction(details.name, details.phase, "check", 0);

            //Bet2 only cheks if it has a hand ranking higher then 1
            if (details.rating >= 2)
                return new PlayerAction(details.name, details.phase, "check", 0);

            //Otherwise it folds
            Fold fold = new Fold();
            return fold.calc(actions, details);
        }
    }

    //Bet behavior
    class Bet : TurnNode
    {
        //This calc impl. determines the value of the bet based off whether the other player checked
        public override PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            //determines the monetary value of the hand
            int estVal = estimateValueOfHand(details);
            Console.WriteLine(estVal);

            //If the other player went first that means their hand wasn't good enough to wager
            if (actions.Count > 0)
            {
                //If this hand is decent, wager a lot
                if (details.rating > 4)
                {
                    details.betAmount += estVal / 2;
                    return new PlayerAction(details.name, details.phase, "bet", estVal / 2);
                }
                //otherwise still wager something because of their bad hand
                details.betAmount += estVal / 4;
                return new PlayerAction(details.name, details.phase, "bet", estVal / 4);
            }
            //If we are first, only wager if we have a decent hand (better then 1)
            else if (details.rating >= 2)
            {
                details.betAmount += estVal / 4;
                return new PlayerAction(details.name, details.phase, "bet", estVal / 4);
            }

            //Otherwise check
            Check check = new Check();
            return check.calc(actions, details);
        }
    }

    //Call behavior
    class Call : TurnNode
    {
        //this calc impl. will match the other players wager if its not too far from the value of our own hand
        public override PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            //determine current values
            Fold fold = new Fold();
            int estVal = estimateValueOfHand(details);
            int actionIndex = actions.Count - 1;
            int inValue = actions[actionIndex].Amount + details.betAmount;

            //if they aren't wagering more than 120% our est. monetary value, then call
            if (inValue <= (estVal + (estVal * .2f)))
            {
                details.betAmount += actions[actionIndex].Amount;
                return new PlayerAction(details.name, details.phase, "call", 0);
            }
            
            //otherwise fold, we're beat
            return fold.calc(actions, details);
        }
    }

    //Raise behavior
    class Raise : TurnNode
    {
        //this calc impl. will raise depending on our hand value, rating and current bet pool
        public override PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            //determine current values
            Call call = new Call();
            int estVal = estimateValueOfHand(details);
            int actionIndex = actions.Count - 1;
            int inValue = actions[actionIndex].Amount + details.betAmount;
            
            //if the current pool commitment isn't moer then double our est. hand value
            if (inValue >= estVal && inValue < estVal * 2)
            {
                //if we have a great hand, wager a lot
                int raiseAmount = (estVal * 2) - inValue;
                if (details.rating > 7)
                {
                    details.betAmount += raiseAmount;
                    return new PlayerAction(details.name, details.phase, "raise", raiseAmount);
                }

                //otherwise just call
                return call.calc(actions, details);
            }

            //if the wager is still low, raise
            if (inValue < (estVal / 2))
            {
                details.betAmount += (estVal / 4);
                return new PlayerAction(details.name, details.phase, "raise", (estVal / 4));
            }

            //if the wager is still low, commit our est. hand value
            if (inValue < estVal)
            {
                details.betAmount += estVal;
                return new PlayerAction(details.name, details.phase, "raise", estVal);
            }

            //otherwise call
            return call.calc(actions, details);
        }
    }

    //Fold behavior
    class Fold : TurnNode
    {
        //this calc impl. returns the fold playeraction
        public override PlayerAction calc(List<PlayerAction> actions, TurnDetails details)
        {
            return new PlayerAction(details.name, details.phase, "fold", 0); ;
        }
    }


    class DiscardNode
    {
        public virtual int Discard(ref Card[] hand, int rating)
        {

            //Hand is really good, unfeasible to try and improve
            //hands are: straight royal flush, straight flush, full house, straight and flush
            if (rating >= 5 && rating != 8) //4 of a kind is excluded, it's also a match hand
            {
                return 0; //no discard
            }
            //match hands are everything left
            else if (rating >= 1)
            {
                return new HasMatches().Discard(ref hand, rating);
            }
            else
            {
                return new HasNothing().Discard(ref hand, rating);
            }
        }
    }
    //has matches: either pairs, 3-of-a-kind or 4 
    //strategy is simple, discard everything that doesn't match
    class HasMatches : DiscardNode
    {
        public override int Discard(ref Card[] hand, int rating)
        {
            int discard = 0;
            //track which indexies need to be discarded
            bool[] toDiscard = { false, false, false, false, false };
            //loop through hand
            for (int i = 0; i < 5; i++)
            {
                //check if card is only one of its value in hand
                if (Evaluate.ValueCount(hand[i].Value, hand) == 1)
                {
                    toDiscard[i] = true;
                    discard++;
                }
            }

            //discarding must be our last action, Evaluate methods will fail if items are null
            if (discard > 0) //if we have things to discard
            {
                for (int j = 0; j < 5; j++) //loop through and discard them
                {
                    if (toDiscard[j]) { hand[j] = null; }
                }
            }

            return discard;
        }
    }
    //has nothing of value, must search for the likelyhood each potential combination individually
    class HasNothing : DiscardNode
    {
        public override int Discard(ref Card[] hand, int rating)
        {
            PartialFlush pf = new PartialFlush();
            PartialStraight ps = new PartialStraight();

            //attempt to find partial flush (will retrun 0 if does not exist)
            //flushes have a higher chance to occur, straight flushes will regardless of which we prioritize
            int discard = pf.Discard(ref hand, rating);
            if (discard > 0)
            {
                return discard;
            }

            //attempt to find partial straight
            discard = ps.Discard(ref hand, rating);
            if (discard > 0)
            {
                return discard;
            }

            //if has not returned partial flush or straight, hand is entirely bad
            //discard all 5 cards
            for (int i = 0; i < 5; i++)
            {
                hand[i] = null;
            }
            return 5;
        }
    }
    class PartialFlush : DiscardNode
    {
        public override int Discard(ref Card[] hand, int rating)
        {
            //search for flush
            string[] suits = { "Hearts", "Clubs", "Diamonds", "Spades" };
            int[] suitCount = { 0, 0, 0, 0 };

            //loop through entire hand
            for (int i = 0; i < 5; i++)
            {
                //loop through every suit, increment respective counter if suit is found
                for (int j = 0; j < 4; j++)
                {
                    if (hand[i].Suit == suits[j]) { suitCount[j]++; }
                }
            }
            //loop through suits and find one with 3 or more
            int discard = 0;
            for (int k = 0; k < 4; k++)
            {
                //find a suit that has at least 3/5 majority
                if (suitCount[k] >= 3)
                {
                    //loop through hand and discard everything that does not match suit
                    for (int l = 0; l < 5; l++)
                    {
                        if (hand[l].Suit != suits[k])
                        {
                            hand[l] = null;
                            discard++;
                        }
                    }
                    k = 4;//end loop
                }
            }

            //if nothing was found, will return 0
            return discard;
        }
    }

    class PartialStraight : DiscardNode
    {
        public override int Discard(ref Card[] hand, int rating)
        {
            int counter = hand[0].Value; //the value of the previous card
            //keep track of which indexies had cards that did not conform
            bool[] nonStraightCards = { false, false, false, false, false };
            int straightCards = 1; //the number of cards seen sofar that are sequential
            int error = 0; //difference between this card and last card
            int totalError = 0;
            //if totalerror is 1 or 2, we will have a closed straight (missing cards in middle)
            //if totalerror is larger, there is no chance to recover

            //because hand is sorted we only need to check the first four,
            for (int i = 1; i < 5; i++)
            {
                error = hand[i].Value - (counter + 1);
                if (error == 0) //perfect lineup
                {
                    straightCards++;
                }
                else if (error == -1) //card has same value
                {
                    //do not increment error, do not count as a straight card
                    nonStraightCards[i] = true;
                }
                else
                {
                    totalError += error; //add errors together
                    if (totalError <= 2)
                    { //total error is still salvagable
                        straightCards++;
                    }
                    else
                    {
                        nonStraightCards[i] = true;
                    }
                }

                //if was exceed error threshold but still have at least 3 cards left,
                //it is possible the first cards were the odd ones out, reset stats and keep going
                if (totalError > 2 || i >= 2)
                {
                    totalError = 0;
                    straightCards = 1;
                    //all previous cards are now out of the straight
                    for (int j = 0; j < i; j++)
                    {
                        nonStraightCards[j] = true;
                    }
                }
            }
            int discard = 0;
            if (straightCards >= 3) //if straight isn't at least 3 long, just ignore it
            {
                //remove all cards that were flagged as not part of the straight
                for (int k = 0; k < 5; k++)
                {
                    if (nonStraightCards[k])
                    {
                        discard++;
                        hand[k] = null;
                    }
                }
            }
            //returns 0 if no straight
            return discard;
        }
    }


    #endregion

}