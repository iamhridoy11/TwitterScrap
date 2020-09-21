using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using tweetingrock1.Classes;
using tweetingrock1.Models;
using Tweetinvi;
using Tweetinvi.Models;
using static tweetingrock1.Classes.CoreTwitterConfiguration;
namespace tweetingrock1.Controllers
{
	public class HomeController : Controller
	{
		CoreTwitterConfiguration config;

		public HomeController(IOptions<CoreTwitterConfiguration> options)
		{
			config = options.Value;
		}
		public IActionResult Index()
		{
			try
			{
				if (String.IsNullOrWhiteSpace(config.TwitterConfiguration.Access_Token)) throw new Tweetinvi.Exceptions.TwitterNullCredentialsException();
				if (String.IsNullOrWhiteSpace(config.TwitterConfiguration.Access_Secret)) throw new Tweetinvi.Exceptions.TwitterNullCredentialsException();
			}
			catch (Tweetinvi.Exceptions.TwitterNullCredentialsException ex)
			{
				return RedirectToAction("AuthenticateTwitter");
			}
			catch (Exception ex)
			{
				// Redirect to your error page here 
			}
			return View();
		}

		public IActionResult AuthenticateTwitter()
		{
			var coreTwitterCredentials = new ConsumerCredentials(
				config.TwitterConfiguration.Consumer_Key
				, config.TwitterConfiguration.Consumer_Secret);
			var callbackURL = "http://" + Request.Host.Value + "/Home/ValidateOAuth";
			var authenticationContext = AuthFlow.InitAuthentication(coreTwitterCredentials, callbackURL);

			return new RedirectResult(authenticationContext.AuthorizationURL);
		}

		public ActionResult ValidateOAuth()
		{
			if (Request.Query.ContainsKey("oauth_verifier") &&
			Request.Query.ContainsKey("authorization_id"))
			{
				var oauthVerifier = Request.Query["oauth_verifier"];
				var authId = Request.Query["authorization_id"];

				var userCredentials =
				AuthFlow.CreateCredentialsFromVerifierCode(oauthVerifier,
				authId);
				var twitterUser =
				Tweetinvi.User.GetAuthenticatedUser(userCredentials);

				config.TwitterConfiguration.Access_Token =
				userCredentials.AccessToken;
				config.TwitterConfiguration.Access_Secret =
				userCredentials.AccessTokenSecret;

				ViewBag.User = twitterUser;
			}

			return View();
		}
		public IActionResult GetHomeTimeline()
		{
			TwitterViewModel homeView = new TwitterViewModel();

			try
			{
				if (config.TwitterConfiguration.Access_Token == null) throw new Tweetinvi.Exceptions.TwitterNullCredentialsException();
				if (config.TwitterConfiguration.Access_Secret == null) throw new Tweetinvi.Exceptions.TwitterNullCredentialsException();

				var userCredentials = Auth.CreateCredentials(
					config.TwitterConfiguration.Consumer_Key
					, config.TwitterConfiguration.Consumer_Secret
					, config.TwitterConfiguration.Access_Token
					, config.TwitterConfiguration.Access_Secret);

				var authenticatedUser = Tweetinvi.User.GetAuthenticatedUser(userCredentials);

				IEnumerable<ITweet> twitterFeed = authenticatedUser.GetHomeTimeline(config.TweetFeedLimit);

				List<TweetItem> tweets = new List<TweetItem>();
				foreach (ITweet tweet in twitterFeed)
				{
					TweetItem tweetItem = new TweetItem();

					tweetItem.Url = tweet.Url;
					tweets.Add(tweetItem);
				}

				homeView.HomeTimelineTweets = tweets;
			}
			catch (Tweetinvi.Exceptions.TwitterNullCredentialsException ex)
			{
				return RedirectToAction("AuthenticateTwitter");
			}
			catch (Exception ex)
			{

			}

			return View("Views/Twitter/HomeTimeline.cshtml", homeView);
		}


		public IActionResult About()
		{
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		public IActionResult Contact()
		{
			ViewData["Message"] = "Your contact page.";

			return View();
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
