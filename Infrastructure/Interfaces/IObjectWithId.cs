using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VeemExercise.Infrastructure
{
    public interface IObjectWithId
    {
        int? Id { get; set; }
    }
}
