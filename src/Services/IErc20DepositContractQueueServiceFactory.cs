﻿namespace Services
{
    public interface IErc20DepositContractQueueServiceFactory
    {
        IErc20DepositContractQueueService Get(string queueName);
    }
}