using HiHi.Serialization;
using HiHi.Common;
using System;
using System.Linq;
using System.Collections.Generic;

/*
 * ANTI-CAPITALIST SOFTWARE LICENSE (v 1.4)
 *
 * Copyright © 2023 Pelle Bruinsma
 * 
 * This is anti-capitalist software, released for free use by individuals and organizations that do not operate by capitalist principles.
 *
 * Permission is hereby granted, free of charge, to any person or organization (the "User") obtaining a copy of this software and associated documentation files (the "Software"), to use, copy, modify, merge, distribute, and/or sell copies of the Software, subject to the following conditions:
 * 
 * 1. The above copyright notice and this permission notice shall be included in all copies or modified versions of the Software.
 * 
 * 2. The User is one of the following:
 *    a. An individual person, laboring for themselves
 *    b. A non-profit organization
 *    c. An educational institution
 *    d. An organization that seeks shared profit for all of its members, and allows non-members to set the cost of their labor
 *    
 * 3. If the User is an organization with owners, then all owners are workers and all workers are owners with equal equity and/or equal vote.
 * 
 * 4. If the User is an organization, then the User is not law enforcement or military, or working for or under either.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT EXPRESS OR IMPLIED WARRANTY OF ANY KIND, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace HiHi {
    public class Question<T> : SyncObject {
        public Action<ushort> OnAsked;
        public Action<ushort, T> OnAnswered;

        public bool AllPeersAnswered => ReceivedAnswers >= expectingAnswers;
        public int ReceivedAnswers => receivedAnswers.Count;
        public int ExpectingAnswers => expectingAnswers;
        /*public T Consensus => receivedAnswers
            .GroupBy(i => i.Value)
            .OrderByDescending(grp => grp.Count())
            .ThenBy(grp => grp.Key)
            .Select(grp => grp.Key)
            .FirstOrDefault();*/

        public T Consensus {
            get {
                IEnumerable<T> answers = receivedAnswers
                    .GroupBy(i => i.Value)
                    .OrderByDescending(grp => grp.Count())
                    .ThenBy(grp => grp.Key)
                    .Select(grp => grp.Key);

                string debugString = string.Empty;

                foreach (T answer in answers) {
                    debugString += $"{answer}, ";
                }

                debugString += $"Picked {answers.FirstOrDefault()}";

                Peer.SendLog(debugString);

                return answers.FirstOrDefault();
            }
        }

        protected override bool RequiresAuthorization => false;

        private Func<T> question;
        private Dictionary<ushort, T> receivedAnswers = new Dictionary<ushort, T>();
        private int expectingAnswers = Peer.Network.PeerIDs.Count;

        public Question(INetworkObject parent, Func<T> question) : base(parent) {
            this.question = question;
        }

        public override void Update() { }

        public void Ask(ushort? destinationPeer = null) {
            ClearReceivedAnswers();

            PeerMessage message = NewMessage(destinationPeer);

            message.Buffer.AddBool(true);

            Peer.SendMessage(message);
        }

        public void Answer(ushort? destinationPeerID = null) {
            PeerMessage message = NewMessage(destinationPeerID);

            message.Buffer.AddBool(false);
            SerializationHelper.Serialize(question.Invoke(), message.Buffer);

            Peer.SendMessage(message);
        }

        public void AnswerSelf() {
            T answer = question.Invoke();

            expectingAnswers++;
            receivedAnswers.Add(Peer.Info.UniqueID, answer);
            OnAnswered?.Invoke(Peer.Info.UniqueID, answer);
        }

        public void ClearReceivedAnswers() {
            receivedAnswers.Clear();
            expectingAnswers = Peer.Network.Connections;
        }

        public override void Synchronize(ushort? destinationPeerID = null) {
            throw new HiHiException($"Question doesn't use {nameof(Synchronize)}. Instead use {nameof(Ask)} and {nameof(Answer)}.");
        }

        public override void Serialize(BitBuffer buffer) {
            base.Serialize(buffer);
        }

        public override void Deserialize(ushort senderPeerID, BitBuffer buffer) {
            bool isQuestion = buffer.ReadBool();

            if (isQuestion) {
                OnAsked?.Invoke(senderPeerID);

                Answer();
            }
            else {
                T receivedAnswer = SerializationHelper.Deserialize<T>(default, buffer);

                receivedAnswers.Add(senderPeerID, receivedAnswer);
                OnAnswered?.Invoke(senderPeerID, receivedAnswer);
            }

            base.Deserialize(senderPeerID, buffer);
        }
    }
}
