using System;
using MiniGame.Network;
using UnityEngine;

namespace Code
{
    public class TestRingBuffer : MonoBehaviour
    {
        private ByteRingBuffer _baseRingBuffer;

        private void Start()
        {
            _baseRingBuffer = new ByteRingBuffer(10);
            _baseRingBuffer.Write(new byte[] { 1, 2, 3, 4, 5 });
            Debug.Log($"new Buffer after write 5: {_baseRingBuffer.GetDebugInfo()}");

            var data = _baseRingBuffer.ReadAllAvailable();
            Debug.Log($"Read data: {BitConverter.ToString(data)}");
            Debug.Log($"Buffer info after read all: {_baseRingBuffer.GetDebugInfo()}");

            _baseRingBuffer.Write(new byte[] { 6, 7, 8, 9 });
            _baseRingBuffer.Write(new byte[] { 10, 11, 12, 13 });
            Debug.Log($"Buffer info after write 8: {_baseRingBuffer.GetDebugInfo()}");

            var head = _baseRingBuffer.Head;
            _baseRingBuffer.Read(5);
            Debug.Log($"Buffer info after read 5: {_baseRingBuffer.GetDebugInfo()}");
            _baseRingBuffer.Head = head;
            Debug.Log($"Buffer after reset head: {_baseRingBuffer.GetDebugInfo()}");

            data = _baseRingBuffer.ReadAllAvailable();
            Debug.Log($"Read data: {BitConverter.ToString(data)}");
            Debug.Log($"Buffer info after read all: {_baseRingBuffer.GetDebugInfo()}");

        }
    }
}