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
    public class Question<T> : SyncObject where T : struct {
        public event Action<ushort> OnQuestionReceived;
        public event Action<ushort, T> OnAnswerReceived;

        public int ExpectingAnswers => answers.Count;
        public int ReceivedAnswers => answers.Where(i => i.Value != null).Count();
        public Dictionary<ushort, T> Answers => answers
            .Where(i => i.Value != null)
            .Select(i => new KeyValuePair<ushort, T>(i.Key, i.Value ?? default))
            .ToDictionary(i => i.Key, i => i.Value);
        public T Consensus {
            get {
                IEnumerable<T> orderedAnswers = answers
                    .Where(i => i.Value != null)
                    .Select(i => i.Value ?? default)
                    .GroupBy(i => i)
                    .OrderByDescending(grp => grp.Count())
                    .ThenBy(grp => grp.Key)
                    .Select(grp => grp.Key);

                return orderedAnswers.FirstOrDefault();
            }
        }

        protected override bool RequiresAuthorization => false;

        private Func<T> question;
        private Dictionary<ushort, T?> answers = new Dictionary<ushort, T?>();

        public Question(INetworkObject parent, Func<T> question) : base(parent) {
            this.question = question;
        }

        public override void Update() { }

        public void Ask(ushort destinationPeer) {
            ExpectAnswer(destinationPeer);

            PeerMessage message = NewMessage(destinationPeer);

            message.Buffer.AddBool(true); // isQuestion

            Peer.SendMessage(message);
        }

        public void Clear() {
            ClearExpectedAnswers();
        }

        public void Answer(ushort? destinationPeerID = null) {
            T answer = question.Invoke();
            PeerMessage message = NewMessage(destinationPeerID);

            message.Buffer.AddBool(false); // isQuestion
            SerializationHelper.Serialize(answer, message.Buffer);

            Peer.SendMessage(message);
        }

        public override void Synchronize(ushort? destinationPeerID = null) {
            throw new HiHiException($"{nameof(Question<T>)} doesn't use {nameof(Synchronize)}. Instead use {nameof(Ask)} and {nameof(Answer)}.");
        }

        public override void Serialize(BitBuffer buffer) {
            base.Serialize(buffer);
        }

        public override void Deserialize(ushort senderPeerID, BitBuffer buffer) {
            bool isQuestion = buffer.ReadBool();

            if (isQuestion) {
                OnQuestionReceived?.Invoke(senderPeerID);

                Answer();
            }
            else {
                T receivedAnswer = SerializationHelper.Deserialize<T>(default, buffer);

                RegisterAnswer(senderPeerID, receivedAnswer);
            }

            base.Deserialize(senderPeerID, buffer);
        }

        public override void OnRegister(byte uniqueID) {
            base.OnRegister(uniqueID);

            Peer.OnDisconnect += HandleDisconnect;
        }

        public override void OnUnregister() {
            base.OnUnregister();

            Peer.OnDisconnect -= HandleDisconnect;
        }

        protected void RegisterAnswer(ushort peerID, T answer) {
            answers[peerID] = answer;
            OnAnswerReceived?.Invoke(peerID, answer);
        }

        protected void ClearExpectedAnswers() {
            answers.Clear();
        }

        protected void ExpectAnswer(ushort peerID) {
            answers.Add(peerID, null);
        }

        protected void UnexpectAnswer(ushort peerID) {
            if (!answers.ContainsKey(peerID)) { return; }

            answers.Remove(peerID);
        }

        protected void HandleDisconnect(ushort peerID, PeerDisconnectReason reason) {
            UnexpectAnswer(peerID);
        }
    }
}
