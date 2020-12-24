using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessEngine
{
	public class PerftHashTable {
		private HashItem[] table;

		public PerftHashTable() {
			ResizeHashTable();
		}

		public void ResizeHashTable() {
			int newSize = Options.HashSize * 1024 * 1024 / Unsafe.SizeOf<HashItem>();
			if (table == null || table.Length != newSize) {
				table = new HashItem[newSize];
			}
		}

		public void ClearHashTable() {
			((Span<HashItem>)table).Fill(default);
		}

		private struct HashItem {
			public ulong hash;
			public uint nodes;
			public byte depth;
		}


		public bool TryLoad(ulong hash, int depth, out ulong nodes) {
			var key = GetKey(hash);
			HashItem item = table[key];
			if (item.hash == hash && item.depth == depth){
				nodes = item.nodes;
				return true;
			}
			nodes = 0;
			return false;
		}

		public void SaveItem(ulong hash, int depth, ulong nodes) {
			var key = GetKey(hash);
			if (table[key].depth <= depth) {
				HashItem item = new HashItem(){
					hash = hash,
					depth = (byte)depth,
					nodes = (uint)nodes
				};
				table[key] = item;
			}
		}

		private int GetKey(ulong hash) {
			return (int)(hash % (ulong)table.Length);
		}
	}
}
