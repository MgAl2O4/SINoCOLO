from nn import NNTraining

training = NNTraining(inputFile='sino-ml-purifyPvE.json', outputFile='sino-ml-purifyPvE.txt')
training.run(numHidden1=64, numEpochs=20)
