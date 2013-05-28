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

    public class Query<T> where T : class,new()
    {
        private Uri baseUri = null;
        private List<string> attributesToLoad = new List<string>();
        private T entityFilter = default(T);
        private List<OrderBy> orderbys = new List<OrderBy>();
        private int? skip = null;
        private int? take = null;
        private List<string> filterAttributes = new List<string>();
        private SelectCriteria select;
        private FilterCriteria where;
        private OrderByCriteria order;

        public class ThenOrderBy
        {
            OrderByCriteria orderBy;
            public ThenOrderBy(OrderByCriteria orderby)
            {
                this.orderBy = orderby;
            }

            public OrderByCriteria ThenBy
            {
                get
                {
                    return orderBy;
                }
            }

            public Query<T> Skip(int skip)
            {
                orderBy.Criteria.Skip(skip);
                return orderBy.Criteria;
            }

            public Query<T> Take(int take)
            {
                orderBy.Criteria.Take(take);
                return orderBy.Criteria;
            }

            private Query<T> Criteria
            {
                get
                {
                    return orderBy.Criteria;
                }
            }
            public static implicit operator Query<T>(ThenOrderBy orderBy)
            {
                return orderBy.Criteria;
            }
        }

        public class OrderByCriteria
        {
            Query<T> criteria;
            ThenOrderBy thenorderby;
            public OrderByCriteria(Query<T> criteria)
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
            Query<T> criteria;
            public AndCondition(Query<T> criteria)
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

            public Query<T> Skip(int skip)
            {
                Criteria.Skip(skip);
                return Criteria;
            }

            public Query<T> Take(int take)
            {
                Criteria.Take(take);
                return Criteria;
            }

            private Query<T> Criteria
            {
                get { return criteria; }
            }

            public static implicit operator Query<T>(AndCondition and)
            {
                return and.Criteria;
            }
        }

        public class FilterCriteria
        {
            Query<T> criteria;
            AndCondition andCondition;
            public FilterCriteria(Query<T> criteria)
            {
                this.criteria = criteria;
                this.andCondition = new AndCondition(criteria);
            }
            private Query<T> Criteria
            {
                get { return criteria; }
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
            Query<T> criteria;

            public SelectCriteria(Query<T> criteria)
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

            public Query<T> Skip(int skip)
            {
                Criteria.Skip(skip);
                return Criteria;
            }

            public Query<T> Take(int take)
            {
                Criteria.Take(take);
                return Criteria;
            }

            private Query<T> Criteria
            {
                get { return criteria; }
            }

            public static implicit operator Query<T>(SelectCriteria select)
            {
                return select.Criteria;
            }
        }

        public Query(Uri serviceUri)
        {
            this.baseUri = serviceUri;
            this.select = new SelectCriteria(this);
            this.where = new FilterCriteria(this);
            this.order = new OrderByCriteria(this);
        }

        public SelectCriteria Select
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

        public Query<T> Skip(int skip)
        {
            this.skip = skip;
            return this;
        }

        public Query<T> Take(int take)
        {
            this.take = take;
            return this;
        }

        private string GetFilterStringForAttribute(string attribute)
        {
            var propInfo = typeof(T).GetProperty(attribute);
            string value = propInfo.GetValue(entityFilter).ToString();
            string formatStringForValue = propInfo.PropertyType == typeof(string) ? "{0} eq '{1}'" : "{0} eq {1}";
            return string.Format(formatStringForValue, attribute, value);
        }

        public override string ToString()
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
                var filterAttributesString = filterAttributes.Select(a => GetFilterStringForAttribute(a));
                string concatenatedFilterAttributesString = string.Concat("$filter=",
                    filterAttributesString.Aggregate((s1, s2) => string.Format("{0} and {1}", s1, s2)), "&");
                sb.Append(concatenatedFilterAttributesString);
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

        private async Task<U> FetchInternal<U>(string url) where U : class
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            // Add an Accept header for JSON format. 
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            // List all products. 
            HttpResponseMessage response = await client.GetAsync(url);  // Blocking call! 

            U item = default(U);
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking! 
                item = await response.Content.ReadAsAsync<U>();
            }
            return item;
        }

        public IEnumerable<T> Execute()
        {
            string endpointUrl = string.Concat(baseUri.AbsoluteUri, typeof(T).Name + "s", this.ToString());
            var results = FetchInternal<IEnumerable<T>>(endpointUrl).Result;
            foreach (var item in results)
            {
                yield return item;
            }
        }
    }

    class Context
    {

        Uri uri = null;
        public Context(string uri)
        {
            this.uri = new Uri(uri);
        }

        private async Task<U> FetchInternal<U>(string url) where U : class
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            // Add an Accept header for JSON format. 
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            // List all products. 
            HttpResponseMessage response = await client.GetAsync(url);  // Blocking call! 

            U item = default(U);
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body. Blocking! 
                item = await response.Content.ReadAsAsync<U>();
            }
            return item;
        }

        public Query<T> CreateQuery<T>() where T : class, new()
        {
            return new Query<T>(uri);
        }

        public IEnumerable<T> Execute<T>(Query<T> query) where T : class, new()
        {
            string endpointUrl = string.Concat(uri.AbsoluteUri, typeof(T).Name + "s", query.ToString());
            var results = FetchInternal<IEnumerable<T>>(endpointUrl).Result;
            foreach (var item in results)
            {
                yield return item;
            }
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
            Context ctx = new Context(@"http://localhost:14379/api/");
            var query = ctx.CreateQuery<Product>()
                            .Select.Attribute(x => x.Name)
                            .Attribute(x => x.Category)
                            .Where.ConditionIs(x => x.Name == "test")
                            .And.ConditionIs(x => x.Category == "test")
                            .And.ConditionIs(x => x.Price == 100)
                            .OrderBy.Ascending(x => x.Price)
                            .ThenBy.Descending(x => x.Category);
            var l = ctx.Execute(query);
        }
    }
}