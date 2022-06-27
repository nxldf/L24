﻿using Blog.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Interfaces
{
    public interface ITranslation<TEntity> where TEntity : class
    {
        Language language { get; set; }
        string languageId { get; set; }
    }

    public interface ITranslatable<TEntity, TTranslation>
        where TTranslation : class, ITranslation<TEntity> where TEntity : class
    {
        ICollection<TTranslation> Translations { get; set; }
    }   
}
