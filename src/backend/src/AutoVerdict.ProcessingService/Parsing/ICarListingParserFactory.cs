namespace AutoVerdict.ProcessingService.Parsing;

public interface ICarListingParserFactory
{
    ICarListingParser GetParser(string listingUrl);
}
