namespace MMCL_CLIRRS;

public class AvlTree<TKey> : IReservationIndex<TKey>
{
    // Private nested node: outside code never touches the tree's internals.
    private class Node
    {
        public TKey Key;
        public List<Reservation> Items = new List<Reservation>();
        public Node? Left, Right;
        public int Height = 1;
        public Node(TKey key) { Key = key; }
    }

    private Node? _root;
    private readonly Func<Reservation, TKey> _keySelector;
    private readonly IComparer<TKey> _comparer;

    public int Count { get; private set; }

    // keySelector says which field this tree is indexed by.
    public AvlTree(Func<Reservation, TKey> keySelector, IComparer<TKey>? comparer = null)
    {
        _keySelector = keySelector;
        _comparer = comparer ?? Comparer<TKey>.Default;
    }

    // ---------- Public operations ----------

    public void Insert(Reservation reservation)
    {
        _root = Insert(_root, reservation);
        Count++;
    }

    public bool Remove(Reservation reservation)
    {
        bool removed = false;
        _root = Remove(_root, reservation, ref removed);
        if (removed) Count--;
        return removed;
    }

    // Exact match: all reservations with this key.
    public IReadOnlyList<Reservation> Find(TKey key)
    {
        Node? node = _root;
        while (node != null)
        {
            int cmp = _comparer.Compare(key, node.Key);
            if (cmp == 0) return node.Items;
            node = cmp < 0 ? node.Left : node.Right;
        }
        return Array.Empty<Reservation>();
    }

    // Range match (e.g. all reservations from Nov 3 to Nov 7), returned in key order.
    public List<Reservation> FindRange(TKey low, TKey high)
    {
        var result = new List<Reservation>();
        CollectRange(_root, low, high, result);
        return result;
    }

    // ---------- Insert / Remove ----------

    private Node Insert(Node? node, Reservation reservation)
    {
        TKey key = _keySelector(reservation);

        if (node == null)
        {
            var created = new Node(key);
            created.Items.Add(reservation);
            return created;
        }

        int cmp = _comparer.Compare(key, node.Key);
        if (cmp < 0) node.Left = Insert(node.Left, reservation);
        else if (cmp > 0) node.Right = Insert(node.Right, reservation);
        else
        {
            node.Items.Add(reservation);   // same key: add to this node's list
            return node;
        }

        return Rebalance(node);
    }

    private Node? Remove(Node? node, Reservation reservation, ref bool removed)
    {
        if (node == null) return null;

        int cmp = _comparer.Compare(_keySelector(reservation), node.Key);
        if (cmp < 0) node.Left = Remove(node.Left, reservation, ref removed);
        else if (cmp > 0) node.Right = Remove(node.Right, reservation, ref removed);
        else
        {
            if (!node.Items.Remove(reservation)) return node;  // not found in this bucket
            removed = true;
            if (node.Items.Count > 0) return node;             // others still share this key

            // Bucket is empty, so delete the node itself.
            if (node.Left == null) return node.Right;
            if (node.Right == null) return node.Left;

            Node successor = MinNode(node.Right);              // two children: use in-order successor
            node.Key = successor.Key;
            node.Items = successor.Items;
            node.Right = RemoveMin(node.Right);
        }

        return Rebalance(node);
    }

    private static Node MinNode(Node node)
    {
        while (node.Left != null) node = node.Left;
        return node;
    }

    private static Node? RemoveMin(Node node)
    {
        if (node.Left == null) return node.Right;
        node.Left = RemoveMin(node.Left);
        return Rebalance(node);
    }

    private void CollectRange(Node? node, TKey low, TKey high, List<Reservation> result)
    {
        if (node == null) return;

        if (_comparer.Compare(low, node.Key) < 0)
            CollectRange(node.Left, low, high, result);

        if (_comparer.Compare(low, node.Key) <= 0 && _comparer.Compare(node.Key, high) <= 0)
            result.AddRange(node.Items);

        if (_comparer.Compare(node.Key, high) < 0)
            CollectRange(node.Right, low, high, result);
    }

    // ---------- AVL balancing ----------

    private static int Height(Node? n) => n?.Height ?? 0;
    private static void UpdateHeight(Node n) => n.Height = 1 + Math.Max(Height(n.Left), Height(n.Right));
    private static int BalanceFactor(Node n) => Height(n.Left) - Height(n.Right);

    private static Node RotateRight(Node y)
    {
        Node x = y.Left!;
        y.Left = x.Right;
        x.Right = y;
        UpdateHeight(y);
        UpdateHeight(x);
        return x;
    }

    private static Node RotateLeft(Node x)
    {
        Node y = x.Right!;
        x.Right = y.Left;
        y.Left = x;
        UpdateHeight(x);
        UpdateHeight(y);
        return y;
    }

    private static Node Rebalance(Node n)
    {
        UpdateHeight(n);
        int balance = BalanceFactor(n);

        if (balance > 1)                                   // left-heavy
        {
            if (BalanceFactor(n.Left!) < 0)                // left-right case
                n.Left = RotateLeft(n.Left!);
            return RotateRight(n);
        }
        if (balance < -1)                                  // right-heavy
        {
            if (BalanceFactor(n.Right!) > 0)               // right-left case
                n.Right = RotateRight(n.Right!);
            return RotateLeft(n);
        }
        return n;
    }
}