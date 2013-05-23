namespace ConsoleClient
{

    public enum OrderByDirection
    {
        Ascending = 0,
        Descending
    }

    public class OrderBy
    {
        public string AttributeName { get; set; }
        public OrderByDirection Direction { get; set; }
        public override string ToString()
        {
            return string.Format("{0}({1})", AttributeName, Direction);
        }
    }

    public interface ICriteria<T>
    {
        T AttributeValuesToMatchAgainst { get; set; }
        string[] AttributesToLoad { get; set; }
        IList<OrderBy> OrderByAttributes { get; set; }
        int? Skip { get; set; }
        int? Take { get; set; }
        string ToString();
    }

    public class CriteriaBuilder<T> where T : class,new()
    {
        public class ThenOrderBy
        {
            OrderByCriteria orderby;
            public ThenOrderBy(OrderByCriteria orderby)
            {
                this.orderby = orderby;
            }

            public OrderByCriteria ThenBy
            {
                get
                {
                    return orderby;
                }
            }
        }

        public class OrderByCriteria
        {
            CriteriaBuilder<T> criteria;
            ThenOrderBy thenorderby;
            public OrderByCriteria(CriteriaBuilder<T> criteria)
            {
                this.criteria = criteria;
                this.thenorderby = new ThenOrderBy(this);
            }

            private void AddOrder(Expression<Func<T, object>> expr, OrderByDirection direction)
            {
                PropertyInfo propInfo = null;
                MemberExpression memberExpr = expr.Body as MemberExpression;
                if (memberExpr != null)
                {
                    propInfo = memberExpr.Member as PropertyInfo;
                }
                else
                {
                    propInfo = (((UnaryExpression)expr.Body).Operand as MemberExpression).Member as PropertyInfo;
                }
                if (propInfo != null)
                {
                    var orderBy = this.criteria.orderbys.SingleOrDefault(o => o.AttributeName == propInfo.Name);
                    if (orderBy != null)
                    {
                        orderBy.Direction = direction;
                    }
                    else
                    {
                        this.criteria.orderbys.Add(new OrderBy { AttributeName = propInfo.Name, Direction = direction });
                    }
                }
            }

            public ThenOrderBy Ascending(Expression<Func<T, object>> expr)
            {
                AddOrder(expr, OrderByDirection.Ascending);
                return thenorderby;
            }
            public ThenOrderBy Descending(Expression<Func<T, object>> expr)
            {
                AddOrder(expr, OrderByDirection.Ascending);
                return thenorderby;
            }
        }

        public class AndCondition
        {
            CriteriaBuilder<T> criteria;
            public AndCondition(CriteriaBuilder<T> criteria)
            {
                this.criteria = criteria;
            }

            public FilterCriteria And
            {
                get
                {
                    return criteria.Where;
                }
            }

            public OrderByCriteria OrderBy
            {
                get
                {
                    return criteria.OrderBy;
                }
            }

        }

        public class FilterCriteria
        {
            CriteriaBuilder<T> criteria;
            AndCondition andCondition;
            public FilterCriteria(CriteriaBuilder<T> criteria)
            {
                this.criteria = criteria;
                this.andCondition = new AndCondition(criteria);
            }

            public AndCondition ConditionIs(Expression<Func<T, object>> filter)
            {
                if (criteria.entityFilter == null)
                {
                    criteria.entityFilter = new T();
                }
                UnaryExpression eprx = filter.Body as UnaryExpression;
                if (eprx != null)
                {
                    BinaryExpression xpr = eprx.Operand as BinaryExpression;
                    if (xpr.NodeType == ExpressionType.Equal)
                    {
                        PropertyInfo propInfo = (xpr.Left as MemberExpression).Member as PropertyInfo;
                        object value = (xpr.Right as ConstantExpression).Value;
                        if (value != null)
                        {
                            propInfo.SetValue(criteria.entityFilter, value);
                            if (!criteria.filterAttributes.Contains(propInfo.Name))
                            {
                                criteria.filterAttributes.Add(propInfo.Name);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Only equality comparisons are supported");
                    }
                }
                return this.andCondition;
            }
        }

        public class SelectCriteria
        {
            CriteriaBuilder<T> criteria;

            public SelectCriteria(CriteriaBuilder<T> criteria)
            {
                this.criteria = criteria;
            }
            public SelectCriteria Attribute(Expression<Func<T, object>> expr)
            {
                PropertyInfo propInfo = null;
                MemberExpression memberExpr = expr.Body as MemberExpression;
                if (memberExpr != null)
                {
                    propInfo = memberExpr.Member as PropertyInfo;
                }
                else
                {
                    propInfo = (((UnaryExpression)expr.Body).Operand as MemberExpression).Member as PropertyInfo;
                }
                if (propInfo != null)
                {
                    var attr = this.criteria.attributesToLoad.SingleOrDefault(a => a == propInfo.Name);
                    if (attr == null)
                    {
                        this.criteria.attributesToLoad.Add(propInfo.Name);
                    }
                }
                return this;
            }

            public FilterCriteria Where
            {
                get
                {
                    return criteria.Where;
                }
            }

            public OrderByCriteria OrderBy
            {
                get
                {
                    return criteria.OrderBy;
                }
            }
        }

        List<string> attributesToLoad = new List<string>();
        T entityFilter = default(T);
        List<OrderBy> orderbys = new List<OrderBy>();
        int? skip = null;
        int? take = null;
        List<string> filterAttributes = new List<string>();
        SelectCriteria select;
        FilterCriteria where;
        OrderByCriteria order;

        public CriteriaBuilder()
        {
            this.select = new SelectCriteria(this);
            this.where = new FilterCriteria(this);
            this.order = new OrderByCriteria(this);
        }

        public SelectCriteria Load
        {
            get
            {
                return select;
            }
        }

        public FilterCriteria Where
        {
            get
            {
                return where;
            }
        }

        public OrderByCriteria OrderBy
        {
            get
            {
                return order;
            }
        }

        private string GetFilterStringForAttribute(string attribute)
        {
            var propInfo = typeof(T).GetProperty(attribute);
            string value = propInfo.GetValue(entityFilter).ToString();
            string formatStringForValue = propInfo.PropertyType == typeof(string) ? "{0} eq '{1}'" : "{0} eq {1}";
            return string.Format(formatStringForValue, attribute, value);
        }

        public string GetCriteriaString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("?");
            if (attributesToLoad.Count > 0)
            {
                string select = string.Concat("$select=",
                    attributesToLoad.Aggregate((s1, s2) => string.Format("{0},{1}", s1, s2)), "&");
                sb.Append(select);
            }
            if (filterAttributes.Count > 0)
            {
                string filter = string.Concat("$filter=",
                    filterAttributes.Aggregate((s1, s2) => string.Format("{0},{1}", GetFilterStringForAttribute(s1), GetFilterStringForAttribute(s2))), "&");
                sb.Append(filter);
            }
            if (orderbys.Count > 0)
            {
                string orderBy = string.Concat("$orderby=",
                    orderbys.Select(o => string.Format("{0} {1}", o.AttributeName, (o.Direction == OrderByDirection.Ascending) ? "asc" : "desc"))
                            .Aggregate((s1, s2) => string.Format("{0},{1}", s1, s2)), "&");
                sb.Append(orderBy);
            }
            return sb.ToString();
        }

        public ICriteria<T> Build()
        {
            var mock = new Moq.Mock<ICriteria<T>>();
            mock.Setup(m => m.Skip).Returns(skip);
            mock.Setup(m => m.Take).Returns(take);
            mock.Setup(m => m.AttributesToLoad).Returns(attributesToLoad.ToArray());
            mock.Setup(m => m.OrderByAttributes).Returns(orderbys);
            mock.Setup(m => m.AttributeValuesToMatchAgainst).Returns(entityFilter);
            mock.Setup(m => m.ToString()).Returns(GetCriteriaString());
            return mock.Object;
        }
    }

    public class Proxy
    {
        Uri baseuri = null;
        public Proxy(string url)
        {
            this.baseuri = new Uri(url);
        }
        public T Fetch<T>(string id) where T : class
        {
            return null;
        }

        public IEnumerable<T> Fetch<T>(ICriteria<T> criteria)
        {
            string criteriaString = criteria.ToString();
            string endpointURI = string.Concat(baseuri.AbsoluteUri, typeof(T).Name, criteriaString);
            //use the generated endpointURI to make the call
            return null;
        }
    }

    public class Product
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public string Category { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            CriteriaBuilder<Product> builder = new CriteriaBuilder<Product>();
            builder.Load.Attribute(x => x.Name)
                        .Attribute(x => x.Category)
                    .Where.ConditionIs(x => x.Name == "test")
                    .And.ConditionIs(x => x.Category == "test")
                    .And.ConditionIs(x => x.Price == 100)
                    .OrderBy.Ascending(x => x.Price)
                    .ThenBy.Descending(x => x.Category);
            var criteria = builder.Build();

            Proxy proxy = new Proxy("http://locahost:2311/api/");
            proxy.Fetch<Product>(criteria);
        }
    }
}