namespace Movie_StructureCode.Domain.Exceptions
{
    public static class ProductException
    {

        // exception when not found Product
        public class ProductNotFoundException : NotFoundException
        {
            public ProductNotFoundException(Guid productId) : 
                base($"The product with the id {productId} was not found")
            {
            }
        }

        //Other exception Product

    }
}
