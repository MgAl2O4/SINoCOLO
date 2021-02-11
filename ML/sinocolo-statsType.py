from nn import NNTraining

training = NNTraining(inputFile='sino-ml-stats.json', outputFile='sino-ml-statsType.txt', labelKey="ctx")
training.run(numHidden1=0, numEpochs=150, codeSuffix='T')
