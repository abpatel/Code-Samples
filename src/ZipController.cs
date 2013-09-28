   public class ZipDistanceDTO
    {
        public string ZipPairs { get; set; }
        public string Distances { get; set; }
    }
    public class ZipDistance
    {
        public string ZipPairs { get; set; }
        public string Distances { get; set; }
    }
    [RoutePrefix("api")]
    public class ZipDistancesController : ApiController
    {
        private readonly IContactsRepository contactRepository;

        // If you are using Dependency Injection, you can delete the following constructor
        public ZipDistancesController()
        {
            SetupMaps();
        }

        private void SetupMaps()
        {           
            Mapper.CreateMap<Models.ZipDistance, DTOs.ZipDistanceDTO>();
            Mapper.CreateMap<DTOs.ZipDistanceDTO, ZipDistance>();

        }
        
        [GET("ZipDistances", RouteName = "GetZipDistances")]
        public IEnumerable<ZipDistanceDTO> GetZipDistances(ODataQueryOptions<ZipDistanceDTO> options)
        {
             //Sample calls:
            //ZipDistances?$select=Distance&$filter=Pairs eq 'Zip1 eq 13240 and Zip2 eq 90210,Zip1 eq 13241 and Zip2 eq 90211'
            //ZipDistances?$select=Distance&$filter=Pairs%20eq%20'Zip1%20eq%2013240%20and%20Zip2%20eq%2090210,Zip1%20eq%2013241%20and%20Zip2%20eq%2090211'

            options.Validate(new ODataValidationSettings{ AllowedQueryOptions= AllowedQueryOptions.Select| AllowedQueryOptions.Filter, AllowedLogicalOperators = AllowedLogicalOperators.And| AllowedLogicalOperators.Equal});
            string rawValue = options.Filter.RawValue.Substring(options.Filter.RawValue.IndexOf("'"), (options.Filter.RawValue.LastIndexOf("'") + 1) - options.Filter.RawValue.IndexOf("'"));
            var commaSplitZipDistancePairs = rawValue.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var tupleZipPairs = commaSplitZipDistancePairs.Select(p => 
                {
                    var zips = p.Split(new[]{"and"},StringSplitOptions.RemoveEmptyEntries);
                    var zipValues = zips.Select( z => z.Split(new[] { "eq" }, StringSplitOptions.RemoveEmptyEntries)[1]).ToArray();
                    return new 
                    { 
                        RawValues = p ,
                        Tuple = new Tuple<string, string>(zipValues[0], zipValues[1])
                    };
                    
                });
            var zipDistances = 
                tupleZipPairs
                .Select(t => 
                    new ZipDistance { ZipPairs = t.RawValues, Distances = GetDistance(t.Tuple.Item1,t.Tuple.Item2) })
                 .ToArray();

            var mappedZipDistances = Mapper.Map<Models.ZipDistance[], DTOs.ZipDistanceDTO[]>(zipDistances);
            return mappedZipDistances;
        }

        private string GetDistance(string p1, string p2)
        {
            return DateTime.Now.Millisecond.ToString();
        }

    }