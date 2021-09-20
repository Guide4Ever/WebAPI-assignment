using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace indigoLabsAssignment.Controllers
{
	[Route("api/region")]
	[ApiController]
	public class ValuesController : ControllerBase
	{
		public int countRows(string[] result) {
			int rows = 0;
			for (int i = 0; i < result.Length && result[i] != null; i++) {
				rows++;
			}
			return rows;
		}
		public string[] GetCSV()
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://raw.githubusercontent.com/sledilnik/data/master/csv/region-cases.csv");
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

			string[] result = new string[10000];
			int index = 0;
			using (StreamReader sr = new StreamReader(resp.GetResponseStream())) 
			{
				string s;			
				do
				{
					s = sr.ReadLine();
					result[index] = s;
					index++;

				} while (s != null);
				sr.Close();
				
			}

			return result;
		}

		public int checkRegionIndex(string region, string headerRow) {

			string row = region.ToLower(); //LJ -> lj
			string[] parsedRow = headerRow.Split(',');
			int appearanceIndex = 0;  //i,i+2,i+3,i+4 (active, deceased, 1st vac, 2nd vac)

			for (int i = 0; i < parsedRow.Length; i++) {
				if (parsedRow[i].Contains(row)) {
					appearanceIndex = i;
					break;
				}
			}
			
	
			return appearanceIndex;
		}

		[HttpGet("cases")]
		public string postCases(string Region, string From, string To) {

			//1. CSV datoteka
			string[] result = GetCSV();
			int numberOfRows = countRows(result);

			//2. OBRAVNAVA Query parametrov (opcijski)
			int yearFrom = int.Parse(result[1].Substring(0, 4));
			int monthFrom = int.Parse(result[1].Substring(5, 2));
			int dayFrom = int.Parse(result[1].Substring(8, 2));
			int yearTo = int.Parse(result[numberOfRows - 1].Substring(0, 4));
			int monthTo = int.Parse(result[numberOfRows - 1].Substring(5, 2));
			int dayTo = int.Parse(result[numberOfRows - 1].Substring(8, 2));

			int startIndex = 0; //izpise vse stolpce od zacetka

			if (From != null)
			{
				yearFrom = int.Parse(From.Substring(0, 4));
				monthFrom = int.Parse(From.Substring(5, 2));
				dayFrom = int.Parse(From.Substring(8, 2));
			}
			if (To != null)
			{
				yearTo = int.Parse(To.Substring(0, 4));
				monthTo = int.Parse(To.Substring(5, 2));
				dayTo = int.Parse(To.Substring(8, 2));
			}

			if (Region != null) {
				startIndex = checkRegionIndex(Region,result[0]);
			}
			DateTime dateFrom = new DateTime(yearFrom, monthFrom, dayFrom, 0, 0, 0);
			DateTime dateTo = new DateTime(yearTo, monthTo, dayTo, 0, 0, 0);

			string words = "";

			for (int i = 1; i < numberOfRows; i++) {
				int year = int.Parse(result[i].Substring(0, 4));
				int month = int.Parse(result[i].Substring(5, 2));
				int day = int.Parse(result[i].Substring(8, 2));
				DateTime dateThis = new DateTime(year, month, day, 0, 0, 0);

				//Znotraj časovnega okvirja
				if (DateTime.Compare(dateThis, dateFrom) >= 0 && DateTime.Compare(dateThis, dateTo) <= 0) {

					//words += result[i]+"\n";
					string[] row = result[i].Split(',');

					if (Region != null)
					{
						words += row[startIndex] + "," + row[startIndex + 2] + "," +
							row[startIndex + 3] + "," + row[startIndex + 4]+"\n";
					}
					else {
						words += result[i]+"\n";
					}


				}
				
			}
			return words;
		}

		[HttpGet("lastweek")]
		public string postLastweek()
		{

			string[] result = GetCSV();
			int numberOfRows = countRows(result);

			//regije
			string[] row = result[0].Split(','); //header
			int index = 0;
			for (int i = 0; i < row.Length; i++)
			{
				if (row[i].Contains("active"))
				{
					index++;
				}
			}
			string[] regions = new string[index];  
			int[] columnActive = new int[index];
			index = 0;
			for (int i = 0; i < row.Length; i++) {
				if (row[i].Contains("active")) {
					string[] temp = row[i].Split('.');
					regions[index] = temp[1];
					columnActive[index] = i;
					index++;
				}
			}
			/*for (int i = 0; i < index; i++) {
				Console.WriteLine("Regija: "+regions[i]+", Stolpec: "+columnActive[i]);
			} */
			int[] sums = new int[index];
			for (int i = numberOfRows-7; i < numberOfRows; i++) {
				string[] tempRow = result[i].Split(',');
				for (int j = 0; j < columnActive.Length; j++) { 
					 sums[j] += int.Parse(tempRow[columnActive[j]]);
				}
			}

			//DESCENDING SORT
			for (int i = 0; i < index - 1; i++)
			{
				for (int j = 0; j < index - i - 1; j++)
				{
					if (sums[j] < sums[j + 1])
					{
						(sums[j], sums[j+1]) = (sums[j+1], sums[j]);          //swapping
						(regions[j], regions[j+1]) = (regions[j+1], regions[j]);
					}
				}
			}
			string words = "";
			for (int i = 0; i < index; i++) {
				words += "Regija: "+regions[i]+" Vsota: "+sums[i]+"\n";
			}
			words += index;
			
			return words;
		}
	}
}
