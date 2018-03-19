using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player6 : Player
    {
        const int ANTE = 20;

        private enum Actions { CHECK, BET, CALL, RAISE, FOLD };
        private enum TurnOrder { FIRST, SECOND};
        
        // risk is a value from -.09 - 0.9
        // risk is calcuted by weighing the projected enemy hand against our own
        float risk = 0.0f;

        // this value represents the hand we believe our opponent to have
        // the scale is from 0.0 - 1.0
        float estimatedEnemyHand = 0.0f;

        Random rng;
        int roundNum;
        int startingMoney;
        int currentMoney;
        int raiseCounter = 0;
        
        int lastActionStr;
        int lastActionAmount;
        Actions currentAction;
        int amountToBet;
        int bluffChance;
        int previousBet;
        int currentBet;

        public Player6(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
            roundNum = 0;
            startingMoney = mny;
            currentMoney = startingMoney;
            rng = new Random();
        }

        //Probabilities: http://www.math.hawaii.edu/~ramsey/Probability/PokerHands.html

        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            // create a test hand to test reactions
            Card[] testHand = new Card[5];
            testHand[0] = new Card("Spades", 4);
            testHand[1] = new Card("Diamonds", 6);
            testHand[2] = new Card("Diamonds", 11);
            testHand[3] = new Card("Spades", 12);
            testHand[4] = new Card("Hearts", 7);

            int test = (int)(((float)rng.Next(33, 100) / 0.01) * 100);
            //Console.Writ

            //info to keep track of
            roundNum = actions.Count;
            
            // evaluate the hand
            Card highCard = null;
            int handStrength = Evaluate.RateAHand(hand, out highCard);

            //update stats
            if (actions.Count > 0) //ai goes second
            {
                //Console.WriteLine("\n*round stats-> First Player: " + actions[0].Name + " total turns taken: " + actions.Count);
                for (int i = 0; i < actions.Count; i++)
                {
                    //Console.WriteLine("   (" + (i) + "): PLR: " + actions[i].Name + ", action: " + actions[i].ActionName + ", inPhase: " + actions[i].ActionPhase + ", amount: " + actions[i].Amount);
                }
            }
            else //ai goes first
            {
                //Console.WriteLine("\n*round stats-> player 1: " + Name + " takes first turn of round.");
            }

            //setup action
            PlayerAction pa = null;

            //start turn
            //Console.WriteLine("\n-> in ai betting round 1");
            //Console.WriteLine("   Total games played: "+roundNum);
            ListTheHand(hand);

            //if ai is first
            // actions available: bet, check, fold
            if (Round1FirstCheck(handStrength, out pa, highCard))
            {
                // we're at round 0, increment so we get an accurate read
                //roundNum++;
                return pa;
            }
            else // the ai is going second, actions available: check, call, raise, fold
            {
                // evaluate the enemy's hand
                estimatedEnemyHand = EvaluateEnemyHand(actions);
                //Console.WriteLine("Estimated enemy hand strength: " + estimatedEnemyHand);
 
                pa = Round1ActionSelector(TurnOrder.SECOND, actions[roundNum - 1], handStrength, highCard);
                return pa;
            }

        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            Console.WriteLine("-> in ai betting round 2");

            // evaluate the hand
            Card highCard = null;
            int handStrength = Evaluate.RateAHand(hand, out highCard);

            //setup action
            Actions action = Actions.BET;
            PlayerAction pa = null;
            Random rng = new Random();

            //keep track of
            float amount = 0;
            int risk = 0;
            int act = 0;
            bool isFirst = true;

            float riskTaking = rng.Next(100); //low=take no risk -- high=take much risk
                                            //probably replace with calculated value based on overall game

            //get previous bet
            for(int i = actions.Count-1; i > 0 ; i--) {
                if(actions[i].ActionPhase == "Bet1" && (actions[i].ActionName == "bet"|| (actions[i].ActionName == "call")|| (actions[i].ActionName == "raise")))
                {
                    previousBet = currentBet = actions[i].Amount; //update last bet (round 1 bet)
                    break;
                }
            }
            //check if first
            if(actions[actions.Count-1].ActionPhase == "Draw") //if was just in last phase
            {
                if (actions[actions.Count - 1].Name == Name) //if ai went last
                {
                    isFirst = false;
                }
            }

            //check hand strength and decide on move
            if (handStrength == 1 || handStrength == 2) {
                if (riskTaking < 100 * 1 / 6)
                {
                    action = Actions.FOLD;
                    Console.WriteLine("AI gives in and folds");
                }
                else if (riskTaking < 100 * 4 / 6) {
                    action = Actions.CHECK;
                    Console.WriteLine("AI plays it safe and checks");
                }
                else if (riskTaking < 100 * 5 / 6) {
                    act = rng.Next(3); //2/3 chance of folding
                    if (act == 0) action = Actions.CHECK;
                    else if (act == 1) action = Actions.CHECK;
                    else if (act == 2) amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is considering taking a risk");
                }
                else {
                    amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is taking a risk");
                }
            }
            if (handStrength > 2 && handStrength <= 5) // 3, 4, 5
            { //check or evaluate risk
                float mod = riskTaking * (handStrength/10 * 0.55f);

                if (riskTaking+mod < 100 * 1 / 10)
                {
                    action = Actions.FOLD;
                    Console.WriteLine("AI gives in and folds");
                }
                else if (riskTaking + mod < 100 * 3 / 6)
                {
                    action = Actions.CHECK;
                    Console.WriteLine("AI plays it safe and checks");
                }
                else if (riskTaking + mod < 100 * 5 / 6)
                {
                    act = rng.Next(100);
                    if (act+mod < 25) action = Actions.CHECK;
                    else if (act + mod < 50 && !isFirst) action = Actions.CALL;
                    else amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is considering taking a risk");
                }
                else
                {
                    amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is taking a risk");
                }
            }
            if (handStrength > 5 && handStrength <= 8) // 6, 7, 8
            {
                //check or evaluate risk
                float mod = riskTaking * (handStrength/10 * 0.7f);

                if (riskTaking + mod < 100 * 1 / 18)
                {
                    action = Actions.CHECK;
                    Console.WriteLine("AI gives in and checks");
                }
                else if (riskTaking + mod < 100 * 1.8 / 6)
                {
                    if (!isFirst) action = Actions.CALL;
                    else amount = Bet(isFirst, handStrength, previousBet, riskTaking*0.8f, out action);

                    Console.WriteLine("AI plays it safe and calls");
                }
                else if (riskTaking + mod < 100 * 5 / 6)
                {
                    act = rng.Next(100);
                    if (act + mod < 10) action = Actions.CHECK;
                    if (act + mod < 50 && !isFirst) action = Actions.CALL;
                    else amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is considering taking a risk");
                }
                else
                {
                    amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is taking a risk");
                }
            }
            if (handStrength == 9 || handStrength == 10) // 9, 10
            { //check or evaluate risk
                //check or evaluate risk
                float mod = riskTaking * (handStrength/10 * 0.9f);

                if (riskTaking + mod < 100 * 4.5 / 10)
                {
                    if(!isFirst)action = Actions.CALL;
                    Console.WriteLine("AI tries to bluff and calls");
                }
                else if (riskTaking + mod < 100 * 7 / 10)
                {
                    act = rng.Next(100);
                    if (act + mod < 50 && !isFirst) action = Actions.CALL;
                    else amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is considering bluffing to raise pot");
                }
                else
                {
                    amount = Bet(isFirst, handStrength, previousBet, riskTaking, out action);
                    Console.WriteLine("AI is confident and raises the bet");
                }
            }

            //end turn and submit action
            //create the PlayerAction
            switch (action)
            {
                case Actions.BET: pa = new PlayerAction(Name, "Bet2", "bet", (int)Math.Ceiling(amount)); break;
                case Actions.RAISE: pa = new PlayerAction(Name, "Bet2", "raise", (int)Math.Ceiling(amount)); break;
                case Actions.CALL: pa = new PlayerAction(Name, "Bet2", "call", (int)Math.Ceiling(amount)); break;
                case Actions.CHECK: pa = new PlayerAction(Name, "Bet2", "check", (int)Math.Ceiling(amount)); break;
                case Actions.FOLD: pa = new PlayerAction(Name, "Bet2", "fold", (int)Math.Ceiling(amount)); break;
                default: Console.WriteLine("Invalid menu selection - try again"); break;
            } Console.WriteLine("< end ai betting round 2 >");

            roundNum++;
            return pa;
        }

        public override PlayerAction Draw(Card[] hand)
        {
            Card curHighCard;
            int curHandStrength = Evaluate.RateAHand(hand, out curHighCard);
            bool takeASmallRisk = false;
            bool takeABigRisk = false;
            
            //Determine if we need to take a risk
            if (estimatedEnemyHand > 0.15f && estimatedEnemyHand <= 0.35f)
            {
                takeASmallRisk = true;
            }
            else if (estimatedEnemyHand > 0.35f)
            {
                takeASmallRisk = true;
                takeABigRisk = true;
            }
            
            
            //Decision Tree (if statements for now)
            PlayerAction pa = null;
            List<int> deleteIndices = new List<int>();
            if (curHandStrength >= 5)   //Do we have a strong hand?
            {
                if (curHandStrength >= 7) //Is it almost unbeatable?
                {
                    if (curHandStrength == 8) //Is it a 4 of a kind?
                    {
                        if (curHighCard.Value > 10) //Get rid of a low high card
                        {
                            pa = new PlayerAction(Name, "Draw", "stand pat", 0);
                        }
                        else
                        {
                            DeleteCards(hand, UnmatchingCards(hand, 4));
                            pa = new PlayerAction(Name, "Draw", "draw", 1);
                        }
                    }
                    else //Nothing to get rid of that wouldn't ruin the hand
                    {
                        pa = new PlayerAction(Name, "Draw", "stand pat", 0);
                    }
                }
                else
                {
                    if (takeABigRisk) //Do we need to take a risk?
                    {
                        if (curHandStrength == 5) //Is it a straight?
                        {
                            if (SimilarSuitedCards(hand, out deleteIndices) == 4) //Is it almost a flush?
                            {
                                DeleteCards(hand, deleteIndices);
                                pa = new PlayerAction(Name, "Draw", "draw", 1);
                            }
                            else
                            {
                                pa = new PlayerAction(Name, "Draw", "stand pat", 0);
                            }
                        }
                        else
                        {
                            if (ConsecutiveCards(hand, out deleteIndices) == 4) //Is it almost a straight flush?
                            {
                                DeleteCards(hand, deleteIndices);
                                pa = new PlayerAction(Name, "Draw", "draw", 1);
                            }
                            else
                            {
                                pa = new PlayerAction(Name, "Draw", "stand pat", 0);
                            }
                        }
                    }
                    else
                    {
                        pa = new PlayerAction(Name, "Draw", "stand pat", 0);
                    }
                }
            }
            else
            {
                if (takeASmallRisk) //Do we need to take a risk?
                {
                    if (curHandStrength > 2) //Is it a medium strength hand?
                    {
                        if (curHandStrength == 4) //Is it a 3 of a kind?
                        {
                            DeleteCards(hand, UnmatchingCards(hand, 3));
                            pa = new PlayerAction(Name, "Draw", "stand pat", 2);
                        }
                        else
                        {
                            if (SimilarSuitedCards(hand, out deleteIndices) >= 3) //Is it almost a flush?
                            {
                                DeleteCards(hand, deleteIndices);
                                pa = new PlayerAction(Name, "Draw", "draw", deleteIndices.Count);
                            }
                            else if (ConsecutiveCards(hand, out deleteIndices) >= 3) //Is it almost a straight?
                            {
                                DeleteCards(hand, deleteIndices);
                                pa = new PlayerAction(Name, "Draw", "draw", deleteIndices.Count);
                            }
                            else // Ditch the 5th card
                            {
                                hand[OddCardOut(hand)] = null;
                                pa = new PlayerAction(Name, "Draw", "stand pat", 1);
                            }
                        }
                    }
                    else
                    {
                        if (SimilarSuitedCards(hand, out deleteIndices) >= 3) //Is it almost a flush?
                        {
                            DeleteCards(hand, deleteIndices);
                            pa = new PlayerAction(Name, "Draw", "draw", deleteIndices.Count);
                        }
                        else if (ConsecutiveCards(hand, out deleteIndices) >= 3) //Is it almost a straight?
                        {
                            DeleteCards(hand, deleteIndices);
                            pa = new PlayerAction(Name, "Draw", "draw", deleteIndices.Count);
                        }
                        else
                        {
                            if (curHandStrength == 2) //Is it a pair?
                            {
                                DeleteCards(hand, UnmatchingCards(hand, 2));
                                pa = new PlayerAction(Name, "Draw", "draw", 3);
                            }
                            else //Ditch all but the high card
                            {
                                for (int i = 0; i < hand.Length; i++)
                                {
                                    if (i != 4)
                                    {
                                        hand[i] = null;
                                    }
                                }
                                pa = new PlayerAction(Name, "Draw", "draw", 4);
                            }
                        }
                    }
                }
                else
                {
                    if (curHandStrength > 2) //Is it a medium strength hand?
                    {
                        if (curHandStrength == 4) //Is it a 3 of a kind?
                        {
                            //Find the high card of the 2 remaining, discard the lower
                            List<int> tempCardList = UnmatchingCards(hand, 3);
                            int deleteIndex = 0;
                            foreach (int i in tempCardList)
                            {
                                if (deleteIndex == 0)
                                {
                                    deleteIndex = i;
                                }
                                else
                                {
                                    if (hand[deleteIndex].Value > hand[i].Value)
                                    {
                                        hand[i] = null;
                                    }
                                    else
                                    {
                                        hand[deleteIndex] = null;
                                    }
                                }
                            }
                            pa = new PlayerAction(Name, "Draw", "draw", 1);
                        }
                        else //If a two pair, ditch the 5th card
                        {
                            hand[OddCardOut(hand)] = null;
                            pa = new PlayerAction(Name, "Draw", "stand pat", 1);
                        }
                    }
                    else
                    {
                        if (curHandStrength == 2) //If a pair, lose the other 3
                        {
                            DeleteCards(hand, UnmatchingCards(hand, 2));
                            pa = new PlayerAction(Name, "Draw", "draw", 3);
                        }
                        else //Discard all but the high card
                        {
                            for (int i = 0; i < hand.Length; i++)
                            {
                                if (i != 4)
                                {
                                    hand[i] = null;
                                }
                            }
                            pa = new PlayerAction(Name, "Draw", "draw", 4);
                        }
                    }
                }
            }
            
            // return the action
            return pa;
        }

        private float EvaluateEnemyHand(List<PlayerAction> enemyActions)
        {
            float estimatedValue = 0.0f;

            // sum up all enemy bets so far
            int totalEnemyBid = 0;
            for (int i = 0; i < enemyActions.Count; i++)
            {
                totalEnemyBid += enemyActions[i].Amount;

                // if the enemy suddenly jumps at a certain point, assume they are trying to trick us
                if (i > 1 && enemyActions[i].Amount - enemyActions[i - 1].Amount > 200)
                {
                    risk -= (float)rng.NextDouble() * (-.1f);
                    estimatedValue += 0.2f;
                }
            }

            if (roundNum > 0)
            { 
                if (enemyActions[roundNum - 1].ActionName == "raise")
                {
                    risk -= (float)rng.NextDouble() * (-.1f);
                    estimatedValue += 0.1f;
                }

                int averageBet = totalEnemyBid / roundNum;

                if (averageBet > 0 && averageBet < 50)
                {
                    risk += (float)rng.NextDouble() * (.1f);
                    estimatedValue-= 0.1f;
                }
                if (averageBet > 49 && averageBet < 100)
                {
                    risk = (float)rng.NextDouble() * (.1f);
                    estimatedValue += 0.2f;
                }
                if (averageBet > 99 && averageBet < 200)
                {
                    risk -= (float)rng.NextDouble() * (-.2f);
                    estimatedValue += 0.4f;
                }
                if (averageBet > 199 && averageBet < 1000)
                {
                    risk -= (float)rng.NextDouble() * (-.4f);
                    estimatedValue += 0.6f;
                }
            }

            return estimatedValue;
        }

        // the behavior tree will analyze which action seems most logical
        // it will then feed that info into the current state which our FSM
        // will process.

        // behavior tree nodes
        private bool Round1FirstCheck(int handStrength, out PlayerAction action, Card highCard)
        {
            action = null;
            // check am I first?
            // if so, step into action selector
            if (roundNum <= 0)
            {
                //currentMoney -= ANTE;
                action = Round1ActionSelector(TurnOrder.FIRST, null, handStrength, highCard);
                return true;
            }
            
            // otherwise return null to represent that we can't act
            return false;
        }

        private PlayerAction Round1ActionSelector(TurnOrder myOrder, PlayerAction prevAction, int handStrength, Card highCard)
        {
            // decalre a param to hold the actual bet
            int bettingAmount;

            // if an action is processed, then return true
            switch(myOrder)
            {
                // all we have to go off of in the first round is our own hand strength
                case TurnOrder.FIRST: // currently in the first turn, the actions we can select are bet, check, and fold
                    if (Round1Check(handStrength, highCard))
                    {
                        currentAction = Actions.CHECK;
                        //lastActionStr = "check";
                        lastActionAmount = 0;
                        return new PlayerAction(Name, "Bet1", "check", 0);
                    }
                    if (Round1Bet(handStrength, out bettingAmount, highCard))
                    {
                        currentAction = Actions.BET;
                        currentMoney -= bettingAmount;
                        lastActionAmount = bettingAmount;
                        return new PlayerAction(Name, "Bet1", "bet", bettingAmount);
                    }
                    if (Round1Fold(handStrength, prevAction))
                    {
                        currentAction = Actions.FOLD;
                        lastActionAmount = 0;
                        return new PlayerAction(Name, "Bet1", "fold", 0);
                    }
                    break;
                case TurnOrder.SECOND: // currently it is the second turn, the actions we can select are call, raise, fold
                    if (Round1PossibleCheck(handStrength, prevAction, highCard))
                    {
                        currentAction = Actions.CHECK;
                        lastActionAmount = 0;
                        return new PlayerAction(Name, "Bet1", "check", 0);
                    }
                    if (Round1PossibleBet(handStrength, prevAction, out bettingAmount, highCard))
                    {
                        currentAction = Actions.BET;
                        lastActionAmount = bettingAmount;
                        return new PlayerAction(Name, "Bet1", "bet", bettingAmount);
                    }
                    if (Round1Raise(handStrength, prevAction, out bettingAmount, highCard))
                    {
                        currentAction = Actions.RAISE;
                        lastActionAmount = bettingAmount;
                        return new PlayerAction(Name, "Bet1", "raise", bettingAmount);
                    }
                    if (Round1Call(handStrength, prevAction, out bettingAmount))
                    {
                        currentAction = Actions.CALL;
                        lastActionAmount = bettingAmount;
                        return new PlayerAction(Name, "Bet1", "call", bettingAmount);
                    }
                    if (Round1Fold(handStrength, prevAction))
                    {
                        currentAction = Actions.FOLD;
                        lastActionAmount = 0;
                        return new PlayerAction(Name, "Bet1", "fold", 0);
                    }
                    break;
                default:
                    Console.WriteLine("error: turn order not specified.");
                    return new PlayerAction(Name, "Bet1", "fold", 0);
            }

            // otherwise something went wrong and return fold
            return new PlayerAction(Name, "Bet1", "fold", 0);
        }

        private bool Round1Bet(int handStrength, out int bettingAmount, Card highCard)
        {
            bettingAmount = 0;          
            // the AI is analyzing whether it should bet for round 1
            // assume value bet, that is, make the highest bet we think the opponent will call
            // first, let's calculate our highest bet we're willing to do, we don't want to scare them off by going all in
            int highestWillingBet = currentMoney / 4;


            if (handStrength == 1 && highCard.Value >= 12)
            {
                int baseBet = highestWillingBet / 6;
                bettingAmount = baseBet;
                return true;
            }

            // determine whether we should bet
            if (handStrength == 2)
            {
                int baseBet = highestWillingBet / 3;
                bettingAmount = baseBet;
                return true;
            }
            else if (handStrength == 3)
            {
                int baseBet = highestWillingBet / 2;
                bettingAmount = baseBet;
                return true;
            }
            else if (handStrength > 3)
            {
                int baseBet = highestWillingBet;
                bettingAmount = baseBet;
                return true;
            }

            return false;
        }
        private bool Round1Check(int handStrength, Card highCard)
        {
            // the AI is analyzing whether it should check for round 1
            if (handStrength == 1 && highCard.Value < 12)
                return true;

            return false;
        }
        private bool Round1Fold(int handStrength, PlayerAction prevAction)
        {
            // the AI is analyzing whether it should fold for round 1
            //if (prevAction != null && prevAction.Amount > (100 * handStrength))
                return true;

            //return false;
        }
        private bool Round1Call(int handStrength, PlayerAction prevAction, out int callAmount)
        {
            int highestWillingBet = handStrength * 50;
            callAmount = 0;
            // the AI is analyzing whether it should call for round 1
            float diffHands = ((float)handStrength * 0.1f) - estimatedEnemyHand;

            // first, check the states in which it can call
            if (prevAction.ActionName == "bet" || prevAction.ActionName == "raise")
            {
                Console.WriteLine("Difference Between Hands: " + diffHands);
                if (prevAction.Amount <= highestWillingBet && diffHands >= (-0.2 + risk))
                {
                    //int amountToBet = handStrength * 25;
                    if (handStrength >= 1)
                    {
                        if (prevAction.Amount < currentMoney)
                            callAmount = 0;
                        else
                            callAmount = 0;

                        currentMoney -= callAmount;
                        return true;
                    }

                }
            }
            return false;
        }
        private bool Round1Raise(int handStrength, PlayerAction prevAction, out int raiseAmount, Card highCard)
        {
            raiseAmount = 0;
            // the AI is analyzing whether it should raise for round 1
            //int highestWillingBet = currentMoney / 2;
            int tempRaiseAmount = handStrength * 50;

            // difference between our hand and the estimated enemy one
            // if the difference in hands is negative then they've maybe got a better hand than us
            float diffHands = ((float)handStrength * 0.1f) - estimatedEnemyHand;

            if (raiseCounter >= handStrength)
                return false;

            if (prevAction.ActionName == "check")
            {
                // if they checked, try to lure them in by offering a low bet
                if (handStrength * 15 < currentMoney)
                    raiseAmount = handStrength * 15;
                else
                    raiseAmount = currentMoney;

                currentMoney -= raiseAmount + prevAction.Amount;
                raiseCounter++;
                return true;
            }

            // first, check the states in which it can raise
            if (prevAction.ActionName == "bet" || prevAction.ActionName == "raise")
            {
                if (diffHands >= (0.1 + risk) && handStrength >= 3)
                {
                    if (tempRaiseAmount / 2 < currentMoney)
                        raiseAmount = rng.Next(tempRaiseAmount / 4, tempRaiseAmount / 2) + (int)(risk * 10);
                    else
                        raiseAmount = currentMoney;

                    currentMoney -= raiseAmount + prevAction.Amount;
                    raiseCounter++;
                    return true;
                }
            

                if (diffHands >= (0.1 + risk) && highCard.Value > rng.Next(9, 11))
                {
                    if (handStrength > 1)
                    {
                        if (tempRaiseAmount / 2 < currentMoney)
                            raiseAmount = rng.Next(tempRaiseAmount / 4, tempRaiseAmount / 2) + (int)(risk * 10);
                        else
                            raiseAmount = currentMoney;

                        currentMoney -= raiseAmount + prevAction.Amount;
                        raiseCounter++;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool Round1PossibleCheck(int handStrength, PlayerAction prevAction, Card highCard)
        {
            // can I check?
            // if so, step into the checking selector
            if (prevAction.ActionName == "check")
                return Round1Check(handStrength, highCard);

            // otherwise return false
            return false;
        }

        private bool Round1PossibleBet(int handStrength,PlayerAction prevAction, out int bettingAmount, Card highCard)
        {
            bettingAmount = 0;

            // can I bet?
            // if so, step into the betting selector
            if (prevAction == null || prevAction.ActionName == "check")
                return Round1Bet(handStrength, out bettingAmount, highCard);

            // otherwise return false
            return false;
        }

        //Get number of cards in the largest represented suit, and cards not part of that suit
        private int SimilarSuitedCards(Card[] hand, out List<int> cardIndices)
        {
            int suitCount = 0;
            List<int> retCardIndices = new List<int>();
            for (int j = 0; j < hand.Length; j++)
            {
                int tempSuitCount = 0;
                List<int> tempRetCardIndeces = new List<int>();
                for (int i = 0; i < hand.Length; i++)
                {
                    if (hand[i].Suit == hand[j].Suit)
                    {
                        tempSuitCount++;
                    }
                    else
                    {
                        tempRetCardIndeces.Add(i);
                    }
                }
                //If there's more of this suit, overwrite the other
                if (tempSuitCount > suitCount)
                {
                    suitCount = tempSuitCount;
                    retCardIndices = tempRetCardIndeces;
                }
            }
            cardIndices = retCardIndices;
            return suitCount;
        }

        //Get number of cards in order, and cards not part of that order
        private int ConsecutiveCards(Card[] hand, out List<int> cardIndices)
        {
            int consecutiveCount = 0;
            List<int> retCardIndices = new List<int>();
            
            if (hand[0].Value == hand[1].Value - 1 &&
               hand[0].Value == hand[2].Value - 2 &&
               hand[0].Value == hand[3].Value - 3)
            {
                consecutiveCount = 4;
                retCardIndices.Add(4);
            }
            else if (hand[1].Value == hand[2].Value - 1 &&
                    hand[1].Value == hand[3].Value - 2 &&
                    hand[1].Value == hand[4].Value - 3)
            {
                consecutiveCount = 4;
                retCardIndices.Add(0);
            }
            else if (hand[0].Value == hand[1].Value - 1 &&
                    hand[0].Value == hand[2].Value - 2 )
            {
                consecutiveCount = 3;
                retCardIndices.Add(3);
                retCardIndices.Add(4);
            }
            else if (hand[1].Value == hand[2].Value - 1 &&
                    hand[1].Value == hand[3].Value - 2)
            {
                consecutiveCount = 3;
                retCardIndices.Add(0);
                retCardIndices.Add(4);
            }
            else if (hand[2].Value == hand[3].Value - 1 &&
                    hand[2].Value == hand[4].Value - 2)
            {
                consecutiveCount = 3;
                retCardIndices.Add(0);
                retCardIndices.Add(1);
            }
            else
            {
                //We don't care if there's less than 3 consecutive, might as well be none (return that)
            }

            cardIndices = retCardIndices;
            return consecutiveCount;
        }

        //Get all cards that aren't part of a matching group
        private List<int> UnmatchingCards(Card[] hand, int numMatchingCards)
        {
            List<int> retCardIndices = new List<int>();
            for (int i = 2; i < 15; i++)
            {
                //Go until we find the designated group
                if (Evaluate.ValueCount(i, hand) == numMatchingCards)
                {
                    for (int j = 0; j < hand.Length; j++)
                    {
                        if (hand[j].Value != i)
                        {
                            retCardIndices.Add(j);
                        }
                    }
                }
            }

            return retCardIndices;
        }

        //Delete list of card indices
        private void DeleteCards(Card[] hand, List<int> cardIndices)
        {
            foreach (int i in cardIndices)
            {
                hand[i] = null;
            }
        }

        //Get the odd card out of a two pair
        private int OddCardOut(Card[] hand)
        {
            int retCard = 0;

            //Get both pairs
            int firstPair = 0;
            int secondPair = 0;
            for (int i = 2; i < 15; i++)
            {
                if (Evaluate.ValueCount(i, hand) == 2)
                {
                    if (firstPair == 0)
                    {
                        firstPair = i;
                    }
                    else
                    {
                        secondPair = i;
                    }
                }
            }

            //Find the 5th card
            for (int i = 0; i < hand.Length; i++)
            {
                if (hand[i].Value != firstPair && hand[i].Value != secondPair)
                {
                    retCard = i;
                }
            }

            return retCard;
        }

        
        private float Bet(bool isFirst, int handStrength, int previousBet, float risk, out Actions action)
        {
            float bet;

            if (isFirst)
            {
                action = Actions.BET;
                //bet upto double of previous bet depending on risk factor
                bet = ( previousBet + ( handStrength / 10 * previousBet ) / 2 ) * 2 *risk/10;
            }
            else
            {
                action = Actions.RAISE;
                //bet upto double of previous bet depending on risk factor
                bet = ( previousBet + ( handStrength / 10 * previousBet ) ) * 2 * risk/10;
            }

            //make sure you dont bet more than you have
            if (Money - bet < 0) bet = Money * ( 0.8f * risk / 10 );

            return bet;
        }
        

        // helper method - list the hand
        // temporarily copied into here so we can see what the AI has
        // TODO: delete when AI is working properly
        private void ListTheHand(Card[] hand)
        {
            // evaluate the hand
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);

            // list your hand
            Console.WriteLine("\nName: " + Name + " Your hand:   Rank: " + rank);
            for (int i = 0; i < hand.Length; i++)
            {
                Console.Write(hand[i].ToString() + " ");
            }
            Console.WriteLine();
        }
    }
}
