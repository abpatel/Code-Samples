public interface IQueryStore
{
    void AddQuery<T>(T query, string queryId);
    T GetQuery<T>(string queryId);
    bool ContainsQuery(string queryId);
    string GenerateKey();
    string[] QueryIds { get; }
}

public class ZipDistance
{
    public string Zip1 { get; set; }
    public string Zip2 { get; set; }
    public string Country { get; set; }
    public string Distance { get; set; }
}

public class ZipDistanceDto
{
    public string Zip1 { get; set; }
    public string Zip2 { get; set; }
    public string Country { get; set; }
    public string Distance { get; set; }
}
public class ZipDistanceQuery
{
    public ZipDistance[] DistancesToQueryFor { get; set; }
}
public class ZipDistanceQueryDto
{
    public ZipDistanceDto[] DistancesToQueryFor { get; set; }
}

[RoutePrefix("api")]
public class ZipDistancesQueryController : ApiController
{
    private IQueryStore store = null;
    public ZipDistancesQueryController(IQueryStore store)
    {
        this.store = store;
        SetupMaps();
    }

    private void SetupMaps()
    {
        Mapper.CreateMap<ZipDistanceQuery, ZipDistanceQueryDto>();
        Mapper.CreateMap<ZipDistanceQueryDto, ZipDistanceQuery>();

    }

    [POST("ZipDistanceQuery", RouteName = "PostZipDistanceQueryRoute")]
    public HttpResponseMessage Post(ZipDistanceQuery item)
    {
        if (ModelState.IsValid)
        {
            string queryID = store.GenerateKey();
            store.AddQuery<ZipDistanceQuery>(item, queryID);
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.Created, item);
            response.Headers.Location = new Uri(Url.Link("PostZipDistancesRoute", new { id = queryID }));
            return response;
        }
        else
        {
            var validationResults = this.ModelState.SelectMany(m => m.Value.Errors.Select(x => x.ErrorMessage + "(Property: " + m.Key + ")"));
            throw new HttpResponseException(this.Request.CreateResponse(HttpStatusCode.BadRequest, validationResults));
        }
    }
}

[RoutePrefix("api")]
public class ZipDistanceQueryResultsController : ApiController
{
    private IQueryStore store = null;
    public ZipDistanceQueryResultsController(IQueryStore store)
    {
        this.store = store;
        SetupMaps();
    }

    private void SetupMaps()
    {
        Mapper.CreateMap<ZipDistanceQuery, ZipDistanceQueryDto>();
        Mapper.CreateMap<ZipDistanceQueryDto, ZipDistanceQuery>();
        Mapper.CreateMap<ZipDistance, ZipDistanceDto>();
        Mapper.CreateMap<ZipDistanceDto, ZipDistance>();


    }

    [GET("ZipDistanceQueryResults/{id}", RouteName = "GetZipDistanceQueryResultsById")]
    public IEnumerable<ZipDistanceDto> GetZipDistancesQueryResults(string id)
    {
        ZipDistanceQuery query = store.GetQuery<ZipDistanceQuery>(id);
        foreach (var dist in query.DistancesToQueryFor)
        {
            dist.Distance = DateTime.Now.Millisecond.ToString();
        }
        var distanceDtos = Mapper.Map<ZipDistance[], ZipDistanceDto[]>(query.DistancesToQueryFor);
        return distanceDtos;
    }

    [GET("ZipDistancesQueryResults", RouteName = "GetZipDistanceQueryResults")]
    public IEnumerable<ZipDistanceDto> GetZipDistancesQueryResults(ODataQueryOptions<ZipDistanceQueryResultDto> options)
    {
        return new ZipDistanceDto[]
            {
                new ZipDistanceDto
                {
                     Zip1 = "121212",
                     Zip2 = "45543",
                     Country = "US"
                }
            };
    }
}
