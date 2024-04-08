using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using dotnet_user.Services.Interface;

namespace dotnet_user.Services
{
    public class SalaryService : ISalaryService
    {
        private readonly ISalaryRepository _salaryRepository;
        private readonly IDateService _dateService;
        public SalaryService(ISalaryRepository salaryRepository, IDateService dateService)
        {
            _salaryRepository = salaryRepository;
            _dateService = dateService;
        }

        public async Task<List<object>> RecordSalaryAndBonus(int id, int year, string password)
        {
            var user = await _salaryRepository.UserInfo(id);
            var userNo = await _salaryRepository.UserNo(id);

            var results = new List<object>();

            var passwordResult = await _salaryRepository.CheckPassword(user.身份證字號, password);

            if (passwordResult)
            {
                var salary = await _salaryRepository.GetSalary(year, userNo.Item2);
                var bonus = await _salaryRepository.GetBonus(year, userNo.Item2);

                for (int i = 1; i <= 12; i++)
                {
                    var month = i.ToString("D2");
                    var salaryMatch = salary.FirstOrDefault(s => month == s.薪資年月.Substring(3, 2))?.薪資年月;
                    var bonusMatch = bonus.FirstOrDefault(b => month == b.獎金年月.Substring(3, 2))?.獎金年月;

                    results.Add(new
                    {
                        第二密碼 = 0,
                        月份 = month,
                        薪資 = salaryMatch ?? string.Empty,
                        獎金 = bonusMatch ?? string.Empty
                    });
                }
            }
            else
            {
                for (int i = 1; i <= 12; i++)
                {
                    results.Add(new
                    {
                        第二密碼 = 1,
                        月份 = i.ToString("D2"),
                        薪資 = string.Empty,
                        獎金 = string.Empty
                    });
                }
            }

            return results;
        }

        public async Task<object> GetSalaryDetail(int id, int year)
        {
            var user = await _salaryRepository.UserInfo(id);
            if (user == null)
            {
                return null;
            }

            var deptName = await _salaryRepository.GetDepartmentName(user.部門別);
            var job = await _salaryRepository.GetJobInformation(user.身份證字號);

            if (job == null)
            {
                return null;
            }

            var (counter, personnelNumber) = await _salaryRepository.UserNo(id);
            if (string.IsNullOrEmpty(personnelNumber))
            {
                return null;
            }

            var salary = await _salaryRepository.GetSalaryDetails(counter, year, personnelNumber);

            decimal debt = 0, credit = 0, food = 0;
            foreach (var item in salary)
            {
                switch (item.加扣項)
                {
                    case "加項":
                        debt += Convert.ToDecimal(item.薪資項目金額);
                        break;
                    case "扣項":
                        credit += Convert.ToDecimal(item.薪資項目金額);
                        break;
                }

                var bonusTypes = new[] { "伙食費", "不休假獎金", "其他(免)", "國定假日加班費", "例假日加班費", "休假日加班費", "加班費" };
                var itemName = item.薪資項目名稱.ToString();
                bool isBonusType = false;

                foreach (var type in bonusTypes)
                {
                    if (itemName == type)
                    {
                        isBonusType = true;
                        break;
                    }
                }

                if (isBonusType)
                {
                    food += Convert.ToDecimal(item.薪資項目金額);
                }
            }

            var userInfoOutput = new
            {
                部門 = deptName,
                代號 = user.人事代號,
                姓名 = user.姓名,
                職務 = job.職務名稱,
                時間 = DateTime.Now.ToString("yyyy-MM"),
                加項 = debt.ToString("N0"),
                扣項 = credit.ToString("N0"),
                應稅 = (debt - food).ToString("N0"),
                實發 = (debt - credit).ToString("N0"),
                發薪 = salary.FirstOrDefault()?.發薪日期,
                帳號 = salary.FirstOrDefault()?.轉帳帳號,
                信箱 = user.Email帳號
            };

            var deptDetailsList = new List<dynamic>();
            var creditDetailsList = new List<dynamic>();

            foreach (var item in salary)
            {
                if (item.加扣項 == "加項")
                {
                    deptDetailsList.Add(new
                    {
                        加項項目 = item.薪資項目名稱 ?? "",
                        加項 = Convert.ToDecimal(item.薪資項目金額).ToString("N0"),
                        加項備註 = item.備註 ?? ""
                    });
                }
                if (item.加扣項 == "扣項")
                {
                    creditDetailsList.Add(new
                    {
                        扣項項目 = item.薪資項目名稱 ?? "",
                        扣項 = Convert.ToDecimal(item.薪資項目金額).ToString("N0"),
                        扣項備註 = item.備註 ?? ""
                    });
                }
            }

            var salaryDetailsList = new List<dynamic>();
            for (int i = 0; i < 9; i++)
            {
                var deptItem = i < deptDetailsList.Count ? deptDetailsList[i] : new { 加項項目 = "", 加項 = "", 加項備註 = "" };
                var creditItem = i < creditDetailsList.Count ? creditDetailsList[i] : new { 扣項項目 = "", 扣項 = "", 扣項備註 = "" };

                salaryDetailsList.Add(new
                {
                    加項項目 = deptItem.加項項目,
                    加項 = deptItem.加項,
                    加項備註 = deptItem.加項備註,
                    扣項項目 = creditItem.扣項項目,
                    扣項 = creditItem.扣項,
                    扣項備註 = creditItem.扣項備註
                });
            }

            var finalOutput = new List<object>
                {
                    new List<object> { userInfoOutput },
                    salaryDetailsList
                };

            return finalOutput;
        }

        public async Task<object> GetBonusDetail(int id, int year)
        {
            var user = await _salaryRepository.UserInfo(id);
            if (user == null)
            {
                return null;
            }

            var deptName = await _salaryRepository.GetDepartmentName(user.部門別);
            var job = await _salaryRepository.GetJobInformation(user.身份證字號);

            if (job == null)
            {
                return null;
            }

            var bonusQuery = _salaryRepository.GetBonusQuery(user.職務代碼, user.身份證字號);
            IEnumerable<dynamic> bonuses = await _salaryRepository.GetBonuses(bonusQuery.Item1, bonusQuery.Item2);

            var debt = bonuses.Sum(b => (decimal)b.實發金額);
            var tax = bonuses.Sum(b => (decimal)b.稅額);

            var userInfo = new
            {
                部門 = deptName ?? "未知部門",
                代號 = user.人事代號,
                姓名 = user.姓名,
                職務 = job.職務名稱,
                時間 = DateTime.Now.ToString("yyyy-MM"),
                加項 = string.Format("{0:n0}", debt),
                應稅 = string.Format("{0:n0}", debt),
                稅額 = string.Format("{0:n0}", tax),
                實發 = string.Format("{0:n0}", debt - tax),
                發薪 = bonuses.Max(b => b.獎金年月),
                帳號 = job.轉帳帳號,
                信箱 = user.Email帳號
            };

            var actualDetails = bonuses.Select(b => (dynamic)new
            {
                加項項目 = b.獎金項目名稱,
                加項 = string.Format("{0:n0}", b.實發金額)
            }).ToList();
            while (actualDetails.Count < 9)
            {
                actualDetails.Add(new { 加項項目 = "", 加項 = "" });
            }

            var result = new object[] { new[] { userInfo }, actualDetails.ToArray() };
            return result;
        }

        public async Task<object> GetDoctorDetail(int id, int year)
        {
            var user = await _salaryRepository.UserInfo(id);
            if (user == null)
            {
                return null;
            }

            var userNoResult = await _salaryRepository.UserNo(id);
            var userNo = userNoResult.Item2;
            if (string.IsNullOrEmpty(userNo))
            {
                return null;
            }

            var lastYear = year / 100 + 1911;
            var lastMonth = year % 100;
            var lastMonthDate = new DateTime(lastYear, lastMonth, 1).AddMonths(-1).ToString("yyyyMM");

            decimal registerAmount = 0;
            decimal clinicAmount = 0;
            decimal admissionAmount = 0;
            decimal medicineAmount = 0;
            decimal noteAmount = 0;
            decimal ownTotal = 0;

            var register = await _salaryRepository.GetRegisterDetail(userNo, year.ToString());
            var clinic = await _salaryRepository.GetClinicDetail(userNo, year.ToString());
            var admission = await _salaryRepository.GetAdmissionDetail(userNo, year.ToString());
            var medicine = await _salaryRepository.GetMedicineDetail(userNo, year.ToString());

            registerAmount = register.Sum(r => (decimal)r.提撥);
            clinicAmount = clinic.Sum(c => (decimal)c.提撥);
            admissionAmount = admission.Sum(a => (decimal)a.提撥);
            medicineAmount = medicine.Sum(m => (decimal)m.提撥);

            var counter = await _salaryRepository.GetPersonnelCounter(userNo);
            IEnumerable<dynamic> notes = await _salaryRepository.GetNotes(counter.FirstOrDefault()?.counter ?? 0, year.ToString());

            foreach (var value in notes)
            {
                noteAmount += (decimal)value.異動金額;
            }

            ownTotal = registerAmount + clinicAmount + admissionAmount + medicineAmount + noteAmount;

            string yearStr = lastMonthDate.ToString();

            var lastYearInt = Convert.ToInt32(yearStr.Substring(0, 4)) - 1911;
            var lastMonthStr = yearStr.Substring(4, 2);

            var formattedLastMonth = $"{lastYearInt}{lastMonthStr}";


            var lastClinics = await _salaryRepository.GetLastClinics(userNo, formattedLastMonth);
            var lastClinicsAmount = await _salaryRepository.GetLastClinicsAmount(userNo, formattedLastMonth);

            var formattedLastClinicsAmount = lastClinicsAmount.Select(x => new
            {
                提成總計 = String.Format("{0:N0}", x.提成總計),
                總額預扣 = String.Format("{0:N0}", x.總額預扣),
                補發或核減 = String.Format("{0:N0}", x.補發或核減),
                給付額 = String.Format("{0:N0}", x.給付額),
                總額追扣 = String.Format("{0:N0}", x.總額追扣),
                實發額 = String.Format("{0:N0}", x.實發額)
            }).FirstOrDefault();

            var lastAdmission = await _salaryRepository.GetLastAdmission(userNo, formattedLastMonth);
            var lastAdmissionAmount = await _salaryRepository.GetLastAdmissionAmount(userNo, formattedLastMonth);

            var formattedLastAdmissionAmount = lastAdmissionAmount.Select(a => new
            {
                提成總計 = string.Format("{0:N0}", a.提成總計),
                總額預扣 = string.Format("{0:N0}", a.總額預扣),
                補發或核減 = string.Format("{0:N0}", a.補發或核減),
                給付額 = string.Format("{0:N0}", a.給付額),
                總額追扣 = string.Format("{0:N0}", a.總額追扣),
                實發額 = string.Format("{0:N0}", a.實發額)
            }).FirstOrDefault();
            var finalResult = new List<object>
                {
                    register.Select(r => new
                    {
                        日期 = r.日期,
                        病歷號碼 = HideNumber(r.病歷號碼),
                        姓名 = HideString(r.姓名),
                        床號 = r.床號,
                        科目 = r.科目,
                        提撥 = r.提撥
                    }).ToList(),
                    string.Format("{0:N0}", registerAmount),
                    clinic.Select(c => new
                    {
                        日期 = c.日期,
                        病歷號碼 = HideNumber(c.病歷號碼),
                        姓名 = HideString(c.姓名),
                        床號 = c.床號,
                        科目 = c.科目,
                        提撥 = c.提撥
                    }).ToList(),
                    string.Format("{0:N0}", clinicAmount),
                    admission.Select(a => new
                    {
                        日期 = a.日期,
                        病歷號碼 = HideNumber(a.病歷號碼),
                        姓名 = HideString(a.姓名),
                        床號 = a.床號,
                        科目 = a.科目,
                        提撥 = a.提撥
                    }).ToList(),
                    string.Format("{0:N0}", admissionAmount),
                    medicine.Select(m => new
                    {
                        日期 = m.日期,
                        病歷號碼 = HideNumber(m.病歷號碼),
                        姓名 = HideString(m.姓名),
                        床號 = m.床號,
                        科目 = m.科目,
                        提撥 = m.提撥
                    }).ToList(),
                    string.Format("{0:N0}", medicineAmount),
                    notes.Select(n => new
                    {
                        備註 = n.備註,
                        金額 = String.Format(CultureInfo.InvariantCulture, "{0:N0}", n.異動金額)
                    }).ToList(),
                    string.Format("{0:N0}", noteAmount),
                    string.Format("{0:N0}", ownTotal),
                    lastClinics.Select(lc => new
                    {
                        病歷號碼 = lc.病歷號碼,
                        姓名 = lc.姓名,
                        執行日期 = lc.執行日期,
                        健保代碼 = lc.健保代碼,
                        治療項目 = lc.治療項目,
                        單價 = lc.單價,
                        總量 = lc.總量,
                        提成比例 = lc.提成比例,
                        提成金額 = lc.提成金額
                    }).ToList(),
                    formattedLastClinicsAmount != null ? formattedLastClinicsAmount : new {},
                    lastAdmission.Select(la => new
                    {
                        病歷號碼 = la.病歷號碼,
                        姓名 = la.姓名,
                        執行日期 = la.執行日期,
                        健保代碼 = la.健保代碼,
                        治療項目 = la.治療項目,
                        單價 = la.單價,
                        總量 = la.總量,
                        提成比例 = la.提成比例,
                        提成金額 = la.提成金額
                    }).ToList(),
                    formattedLastAdmissionAmount != null ? formattedLastAdmissionAmount : new {}
                };

            return finalResult;
        }

        private string HideNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return number;
            var firstStr = number.Substring(0, 2);
            var lastStr = number.Length > 1 ? number.Substring(number.Length - 1) : "";
            var masked = firstStr + new string('*', Math.Max(0, number.Length - 3)) + lastStr;
            return masked;
        }

        private string HideString(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var firstChar = name.Substring(0, 1);
            var lastChar = name.Length > 1 ? name.Substring(name.Length - 1) : "";
            var masked = firstChar + new string('*', Math.Max(0, name.Length - 2)) + lastChar;
            return masked;
        }
    }
}