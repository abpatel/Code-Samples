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
    }

    public class CriteriaBuilder<T> where T : class,new()
    {
        T entityFilter = default(T);
        List<string> attributesToLoad = new List<string>();
        List<OrderBy> orderbys = new List<OrderBy>();
        int? skip = null;
        int? take = null;
        List<string> filterAttributes = new List<string>();

        public CriteriaBuilder<T> AddFilter(Expression<Func<T, object>> filter)
        {
            if (entityFilter == null)
            {
                entityFilter = new T();
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
                        propInfo.SetValue(entityFilter, value);
                        if (!filterAttributes.Contains(propInfo.Name))
                        {
                            filterAttributes.Add(propInfo.Name);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("Only equality comparisons are supported");
                }
            }
            return this;
        }

        public CriteriaBuilder<T> LoadAttribute(Expression<Func<T,object>> expr)            
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
               var attr = attributesToLoad.SingleOrDefault(a => a == propInfo.Name);
               if (attr == null)
               {
                   attributesToLoad.Add(propInfo.Name);
               }
            }
            return this;
        }

        public void AddOrderBy(Expression<Func<T, object>> expr, OrderByDirection direction)
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
                var orderBy = orderbys.SingleOrDefault(o => o.AttributeName == propInfo.Name);
                if (orderBy != null)
                {
                    orderBy.Direction = direction;
                }
                else
                {
                    this.orderbys.Add(new OrderBy { AttributeName = propInfo.Name, Direction = direction });
                }
            }
        }

        public CriteriaBuilder<T> OrderByDescending(Expression<Func<T, object>> expr)
        {
            AddOrderBy(expr, OrderByDirection.Descending);
            return this;
        }

        public CriteriaBuilder<T> OrderByAscending(Expression<Func<T, object>> expr)
        {
            AddOrderBy(expr, OrderByDirection.Ascending);
            return this;
        }

        public CriteriaBuilder<T> Skip(int skip)
        {
            this.skip = skip;
            return this;
        }

        public CriteriaBuilder<T> Take(int take)
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

        public string GetCriteriaString()
        {
            StringBuilder sb = new StringBuilder();
            if (attributesToLoad.Count > 0)
            {
                string select = string.Concat("$select=", 
                    attributesToLoad.Aggregate((s1, s2) => string.Format("{0},{1}", s1, s2)),"&");
                sb.Append(select);
            }
            if (filterAttributes.Count > 0)
            {
                string filter = string.Concat("$filter=",
                    filterAttributes.Aggregate((s1,s2) => string.Format("{0},{1}",GetFilterStringForAttribute(s1),GetFilterStringForAttribute(s2))),"&");
                sb.Append(filter);
            }
            if (orderbys.Count > 0)
            {
                string orderBy = string.Concat("$orderby=",
                    orderbys.Select(o => string.Format("{0} {1}",o.AttributeName, (o.Direction == OrderByDirection.Ascending) ? "asc" : "desc"))
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
}