using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
//namespace RegistrationAndLogin.Models.Extended
namespace AspNetIdentity.Models
{
		[MetadataType(typeof(UserMetadata))]
		public partial class User
		{
				public string ConfirmPassword { get; set; }
		}

		public class UserMetadata
		{
				[Display(Name = "First Name")]
				[Required(AllowEmptyStrings = false, ErrorMessage = "First name required")]
				public string FirstName { get; set; }

				[Display(Name = "Last Name")]
				[Required(AllowEmptyStrings = false, ErrorMessage = "Last name required")]
				public string LastName { get; set; }

				[Display(Name = "Email ID")]
				[Required(AllowEmptyStrings = false, ErrorMessage = "Email ID required")]
				[DataType(DataType.EmailAddress)]
				public string EmailID { get; set; }

				[Display(Name = "Date of birth")]
				[DataType(DataType.Date)]
				[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:MM/dd/yyyy}")]
				public DateTime DateOfBirth { get; set; }

				[Required(AllowEmptyStrings = false, ErrorMessage = "Password is required")]
				[DataType(DataType.Password)]
				[MinLength(6, ErrorMessage = "Minimum 6 characters required")]
				public string Password { get; set; }

				[Display(Name = "Confirm Password")]
				[DataType(DataType.Password)]
				[Compare("Password", ErrorMessage = "Confirm password and password do not match")]
				public string ConfirmPassword { get; set; }

		}
}