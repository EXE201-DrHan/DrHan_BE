﻿namespace DrHan.Domain.Exceptions
{
    public class UpdateFailedException(string resourceName) : Exception($"Update resource {resourceName} failed")
    {
    }
}
