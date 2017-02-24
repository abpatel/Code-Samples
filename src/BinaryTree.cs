using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructuresAndAlgorithms
{
    class BinaryTreeNode<T> where T: IComparable<T>
    {
        public BinaryTreeNode(T value)
        {
            this.Value = value;
        }
        public T Value { get; private set; }
        public BinaryTreeNode<T> Left { get; set; }
        public BinaryTreeNode<T> Right { get; set; }
    }
    class BinaryTree<T>  : ICollection<T> where T: IComparable<T>
    {
        private int count = 0;
        private BinaryTreeNode<T> root = null;
        public BinaryTree()
        {
            
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        private void AddInternal(T item)
        {
            if(root == null)
            {
                root = new BinaryTreeNode<T>(item);
            }

            BinaryTreeNode<T> current = root;
            while (current != null)
            {
                if (current.Value.CompareTo(item) <= 0)
                {
                    if(current.Right == null)
                    {
                        current.Right = new BinaryTreeNode<T>(item);
                        break;
                    }
                    else
                        current = current.Right;
                }
                else
                {
                    if(current.Left == null)
                    {
                        current.Left = new BinaryTreeNode<T>(item);
                        break;
                    }
                    else
                        current = current.Left;
                }
            }
        }

        private bool BinarySearch(BinaryTreeNode<T> node, T item)
        {
            var current = node;
            bool found = false;
            if (current != null)
            {
                if (current.Value.CompareTo(item) == 0)
                    found = true;
                if (current.Value.CompareTo(item) < 0)
                {
                    return BinarySearch(current.Right, item);
                }
                else
                {
                    return BinarySearch(current.Left, item);
                }
            }
            return found;
        }

        public void Add(params T[] items)
        {
            foreach (var item in items)
            {
                AddInternal(item);
            }
        }

        public void Add(T item)
        {
            AddInternal(item);
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            this.root = null;
            this.count = 0;
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<T> InOrderIterative()
        {
            if (root != null)
            {
                Stack<BinaryTreeNode<T>> stack = new Stack<BinaryTreeNode<T>>();
                var tos = root;
                stack.Push(root);
                while(tos != null)
                {
                    if (tos.Left != null)
                    {
                       stack.Push(tos.Left);
                    }
                    else
                    {
                        tos = stack.Pop();
                        yield return tos.Value;
                        if (tos.Right != null)
                        {
                            stack.Push(tos.Right);
                        }
                    }
                    //tos = stack.Peek();
                }
            }
        }

        private IEnumerable<T> InOrderRecursive(BinaryTreeNode<T> node)
        {
            var current = node;
            if(current != null)
            {
                foreach (var item in InOrderRecursive(current.Left))
                    yield return item;
                yield return node.Value;
                foreach (var item in InOrderRecursive(current.Right))
                    yield return item;
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            var enumerable = InOrderRecursive(root);
            foreach (var item in enumerable)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class Prg
    {
        static void Main(string[] args)
        {

            BinaryTree<int> tree = new BinaryTree<int>();
            tree.Add(5, 4, 3, 7, 2, 1, 9);
            foreach (var item in tree)
            {
                Console.WriteLine(item);
            }
            Console.ReadLine();
        }
    }
}
