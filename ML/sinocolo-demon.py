from nn import NNTraining

training = NNTraining(inputFile='sino-ml-demon.json', outputFile='sino-ml-demon.txt')
training.run(numHidden1=64, numEpochs=20)
