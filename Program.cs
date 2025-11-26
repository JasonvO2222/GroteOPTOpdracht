
namespace GroteOPTOpdracht
{
    public class Program
    {

        public static void Main(string[] args)
        {
            //initialize datastructures
            int[,,] afstandenMatrix = new int[1099, 1099, 2];
            List<Order> orderList = new List<Order>();

            // parse the text files
            StreamReader afstanden = new StreamReader("AfstandenMatrix.txt");
            string line = afstanden.ReadLine();
            line = afstanden.ReadLine();

            while (line != null) {
                string[] splt = line.Split(';');
                int i = int.Parse(splt[0]);
                int j = int.Parse(splt[1]);
                afstandenMatrix[i, j, 0] = int.Parse(splt[2]);
                afstandenMatrix[i, j, 1] = int.Parse(splt[3]);
                line = afstanden.ReadLine();
            }

            StreamReader orders = new StreamReader("Orderbestand.txt");
            line = orders.ReadLine();
            line = orders.ReadLine();

            while (line != null) {
                Order ord = new Order(line);
                orderList.Add(ord);
            }



        }
    }
}