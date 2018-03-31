using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player4 : Player
    {
        Random rand;//Random number generator to determine what actions are taken based on confidence probability
        float confidence;//Number between 0-1 that represents confidence that have winning hand

        int currentRoundNumber;//Number of the round we're currently on
        int opponentMoney;//Keeps track of how much money the opponent has

        Round currentRound;//Object keeps track of important info about this round

        float[] handProbability = {
            0.5f,//0 - high card, 50% chance of occurring
            0.58f,//1 - one pair, 42% chance of occurring
            0.95f,//2 - two pair, ~5% chance of occurring
            0.98f,//3 - three of a kind, ~2% chance of occurring
            1//4+ - straight or better, <1% chance of occurring
        };

        public Player4(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
            rand = new Random();//Rand object for any random number generation we need to do

            currentRoundNumber = 0;//Goes from 1-100
            opponentMoney = mny;//Assume they start with the same amt of money as us

            //currentRound = new Round(0);//Start with round 0
        }

        //Start a new round
        private void NewRound()
        {
            currentRound = new Round(++currentRoundNumber);            
        }

        //Actions for betting round 1
        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            Evaluate.SortHand(Hand);//Make sure hand is sorted

            confidence = GetHandConfidence();//Get confidence for current hand

            int maxBet = (int)(GetMaxBet() * confidence);//Maximum amt we're willing to bet... This probably isn't a good option right now

            //If they're going first, get their action so we can factor that in
            //Can call, raise, or fold
            if (actions.Count > 0)
            {
                if(actions.Count <= 1)
                {
                    NewRound();
                }

                PlayerAction opponentAction = actions.Last();

                int oppBetAmt = 0;//How much opponent just bet

                //If the opponent bet/raised, determine whether to call, raise, or fold
                //Calling will end the betting round
                //Raising will send it back around to the opponent
                //Folding will obviously mean we lose
                if (opponentAction.ActionName == "bet" || opponentAction.ActionName == "raise")
                {
                    oppBetAmt = opponentAction.Amount;
                    opponentMoney -= oppBetAmt;
                    currentRound.AddOpponentBet(oppBetAmt);

                    //How much have they bet? Would the amount of money we'd have to pay to match them be worth the risk?
                    float potOdds = GetPotOdds();

                    //If we don't have enough money to match, fold
                    //Otherwise, if the pot odds are bad and our confidence is low, randomly decide whether we're going to fold based on confidence
                    //Lower confidence = more likely to fold
                    if (oppBetAmt > Money || (potOdds < confidence && rand.NextDouble() > confidence))
                    {
                        return new PlayerAction(Name, "Bet1", "fold", 0);
                    }
                    //We're staying in! Random chance of only calling based on confidence
                    else if (rand.NextDouble() > confidence)
                    {
                        return new PlayerAction(Name, "Bet1", "call", 0);
                    }
                    //If not folding or calling, raise
                    else
                    {
                        //Get amount to raise by as random number between 0 and absolute max of 25
                        //Varies based on confidence rating
                        int betAmt = GetAmountToBet();

                        return new PlayerAction(Name, "Bet1", "raise", betAmt);
                    }
                }
                //The only other option is that they're checking
                else//opponentAction.ActionName == "check"
                {
                    //Wow, they checked right off the bat?
                    //Increase confidence by 10% because they probably have a bad hand
                    confidence = Math.Min(1f, confidence * 1.1f);

                    //Definitely don't fold! Decide whether to check as well or raise
                    if (rand.NextDouble() > confidence)
                    {
                        return new PlayerAction(Name, "Bet1", "check", 0);
                    }
                    //If not folding or calling, raise
                    else
                    {
                        //Get amount to raise by as random number between 0 and absolute max of 25
                        //Varies based on confidence rating
                        int betAmt = GetAmountToBet();

                        return new PlayerAction(Name, "Bet1", "raise", betAmt);
                    }
                }
            }
            //If we're going first, will have to base decision entirely off of hand strength
            //Can check, bet, or fold
            else
            {
                NewRound();//Start new round

                //Determine what to do, we're going first
                if (rand.NextDouble() > confidence)
                {
                    return new PlayerAction(Name, "Bet1", "check", 0);
                }
                //If not folding or checking, bet
                //Get amount to bet as random number between 0 and absolute max of 25
                //Varies based on confidence rating
                else
                {
                    int betAmt = GetAmountToBet();

                    return new PlayerAction(Name, "Bet1", "bet", betAmt);
                }      
            }
        }

        //Actions for draw round
        public override PlayerAction Draw(Card[] hand)
        {
            return new PlayerAction(Name, "Draw", "draw", 0);
        }

        //Implemented by Mark Scott
        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            return new PlayerAction(Name, "Bet2", "bet", 10);//Placeholder
        }

        //Calculate maximum amt we're willing to bet this round
        //As it is right now, I don't think this is a great solution
        private int GetMaxBet()
        {
            return Money / 15 - currentRound.PlayerBetAmt;
        }

        //Randomly determine what to bet based on confidence
        private int GetAmountToBet()
        {
            //Absolute max is 25 when at max confidence
            return rand.Next(1, (int)(25 * confidence));
        }

        //Calculate confidence in hand
        private float GetHandConfidence()
        {
            Card highCard;

            //Clamp rating to value btwn 0-4
            //0 is high card, 4 is a straight or better - we treat 4+ hands all functionally the same since they're all such insanely rare/good hands
            int rating = Math.Min(Evaluate.RateAHand(Hand, out highCard) - 1, 4);

            float conf = handProbability[rating];

            //If we have a high card, modify confidence based on its value
            //Maximum confidence possible for a high card is 50%
            if(rating <= 1)
            {
                conf *= (highCard.Value-1) / 13;
            }

            //If we have pairs, modify confidence by the strength of these pairs
            else if(rating >= 2 && rating <= 3)
            {
                //rating - 1 = number of pairs
                //Strength of pairs is value between 0-1, multiply it by the range between probability rating of this hand and the next worst hand
                //Sssentially, a pair of 2s is only barely better than a high Ace, while a pair of Aces is almost as good as the worst three of a kind
                conf += GetStrengthOfPairs(rating - 1) * (handProbability[rating] - handProbability[rating - 1]);
            }

            return conf;
        }

        //Use this to determine how strong our hand is if it's a one pair/two pair
        private float GetStrengthOfPairs(int numPairs)
        {
            float strength = 0;

            //Check for pairs within cards and increase strength based on value of any pairs found
            //Since they're pre-sorted we know pairs will be right next to each other in a hand
            for(int i = 0; i < 5; ++i)
            {
                if(Hand[i].Value == Hand[i+1].Value)
                {
                    //Strength of pair is determined by value where 2 is worth ~0.076 and A is worth 1
                    //If we're evaluating a two pair then get average strength of them both
                    strength += (Hand[i++].Value-1) / (13 * numPairs);
                }
            }

            return strength;//Return strength as float
        }

        //Look at the ratio for how much you'll have to pay vs the potential payout if you win
        //Lower pot odds are less worth the risk - balance this with confidence in hand
        private float GetPotOdds()
        {
            //Divide total pot by amount you need to spend to call
            //Invert percentage so that higher % is better
            return 1 - ((currentRound.OpponentBetAmt - currentRound.PlayerBetAmt) / currentRound.Pot);
        }

    }

    class Round
    {
        private int ante;//Ante of current round, increases as game goes on
        private int pot;//Pot for current roud, increases as players bet
        private int opponentBetAmt;//How much the opponent has bet this round so far
        private int playerBetAmt;//How much we have bet this round so far

        //Props
        public int Ante { get { return ante; } }
        public int Pot { get { return pot; } }
        public int OpponentBetAmt { get { return opponentBetAmt; } }
        public int PlayerBetAmt { get { return playerBetAmt; } }

        //Constructor takes current round number
        public Round(int roundNum)
        {
            //Calculate ante based on current round
            if (roundNum > 50)
            {
                ante = 30;
            }
            else if (roundNum > 25)
            {
                ante = 20;
            }
            else
            {
                ante = 10;
            }

            pot = ante * 2;

            opponentBetAmt = 0;
            playerBetAmt = 0;
        }

        public void AddPlayerBet(int amt)
        {
            playerBetAmt += amt;
            pot += amt;
        }

        public void AddOpponentBet(int amt)
        {
            opponentBetAmt += amt;
            pot += amt;
        }
    }
}

