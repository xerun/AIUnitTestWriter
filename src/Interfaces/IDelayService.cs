﻿namespace AIUnitTestWriter.Interfaces
{
    public interface IDelayService
    {
        Task DelayAsync(int milliseconds, CancellationToken cancellationToken = default);
    }
}
